using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Paperless.Api.Dto.Requests;
using Paperless.Api.Dto.Responses;

namespace Paperless.Api.Auth;

/// <summary>
/// Token authentication controller — compatible with the DRF TokenObtainPairView
/// and the Angular SPA authentication flow.
///
/// <para>Endpoints:</para>
/// <list type="bullet">
///   <item><c>POST /api/token/</c> — obtain a JWT by username/password</item>
///   <item><c>POST /api/token/refresh/</c> — refresh (re-issue) a valid token</item>
///   <item><c>POST /api/token/verify/</c> — verify a token's validity</item>
/// </list>
/// </summary>
[ApiController]
[Route("/api/token")]
public class TokenController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<TokenController> _logger;

    public TokenController(
        IAuthService authService,
        IOptions<JwtOptions> jwtOptions,
        ILogger<TokenController> logger)
    {
        _authService = authService;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/token/ — login with username and password, receive a JWT.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A JWT token on success, or 401 Unauthorized on failure.</returns>
    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.ValidateCredentialsAsync(
            request.Username, request.Password);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Login failed for user {Username}: {Error}",
                request.Username, result.Error.Code);

            return Unauthorized(new
            {
                detail = result.Error.Message
            });
        }

        var user = result.Value;
        var token = GenerateToken(user);

        _logger.LogInformation(
            "Token issued for user {Username} (id={UserId})",
            user.Username, user.Id);

        return Ok(new TokenResponse(token));
    }

    /// <summary>
    /// POST /api/token/refresh/ — refresh (re-issue) a valid JWT.
    /// The provided token must not be expired. A new token is issued with
    /// extended expiration.
    /// </summary>
    /// <param name="request">The current token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new JWT token on success, or 401 on failure.</returns>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var principal = ValidateToken(request.Token);
        if (principal is null)
        {
            return Unauthorized(new { detail = "Invalid or expired token." });
        }

        var userIdClaim = principal.FindFirst("user_id")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { detail = "Token does not contain a valid user ID." });
        }

        var userResult = await _authService.GetUserByIdAsync(userId);
        if (userResult.IsFailure)
        {
            return Unauthorized(new { detail = userResult.Error.Message });
        }

        var token = GenerateToken(userResult.Value);

        _logger.LogInformation(
            "Token refreshed for user {Username} (id={UserId})",
            userResult.Value.Username, userResult.Value.Id);

        return Ok(new TokenResponse(token));
    }

    /// <summary>
    /// POST /api/token/verify/ — verify the validity of a JWT token.
    /// </summary>
    /// <param name="request">The token to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with <c>{ valid: true }</c> or 401 with <c>{ valid: false }</c>.</returns>
    [AllowAnonymous]
    [HttpPost("verify")]
    [ProducesResponseType(typeof(TokenVerifyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(TokenVerifyResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult Verify([FromBody] VerifyTokenRequest request)
    {
        var principal = ValidateToken(request.Token);
        if (principal is null)
        {
            return Unauthorized(new TokenVerifyResponse { Valid = false });
        }

        return Ok(new TokenVerifyResponse { Valid = true });
    }

    /// <summary>
    /// Generates a JWT token for the given authenticated user.
    /// The token contains: user_id, username, roles, exp, iat.
    /// </summary>
    private string GenerateToken(AuthenticatedUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new("user_id", user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("username", user.Username),
            new(ClaimTypes.AuthenticationMethod, "Token"),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add role claims
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: now.AddMinutes(_jwtOptions.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validates a JWT string and returns the <see cref="ClaimsPrincipal"/>,
    /// or null if validation fails.
    /// </summary>
    private ClaimsPrincipal? ValidateToken(string tokenString)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidAudience = _jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_jwtOptions.Key)),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(tokenString, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Token validation failed");
            return null;
        }
    }
}
