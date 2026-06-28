using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Paperless.Api.Auth;

/// <summary>
/// Custom AuthenticationHandler that supports the DRF-compatible
/// <c>Authorization: Token &lt;jwt&gt;</c> header format.
///
/// <para>
/// This handler parses the <c>Token</c> scheme (e.g. <c>Authorization: Token eyJ... </c>)
/// and validates the embedded JWT using the same signing key, issuer, and audience
/// as the standard JWT Bearer handler. On success it creates a <see cref="ClaimsPrincipal"/>
/// with <c>user_id</c>, <c>username</c>, and <c>role</c> claims.
/// </para>
///
/// <para>
/// Registration in DI (see <c>ServiceCollectionExtensions</c>):
/// <code>
///   services.AddScheme&lt;AuthenticationSchemeOptions, TokenAuthHandler&gt;("Token", null);
/// </code>
/// </para>
/// </summary>
public class TokenAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<TokenAuthHandler> _logger;

    public TokenAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        IOptions<JwtOptions> jwtOptions)
        : base(options, loggerFactory, encoder)
    {
        _jwtOptions = jwtOptions.Value;
        _logger = loggerFactory.CreateLogger<TokenAuthHandler>();
    }

    /// <summary>
    /// Handles the authentication by extracting a JWT from the
    /// <c>Authorization: Token &lt;token&gt;</c> header and validating it.
    /// </summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 1. Extract the Authorization header
        if (!Request.Headers.TryGetValue("Authorization", out var authHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authHeader = authHeaderValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // 2. Check for the "Token " scheme prefix (case-insensitive)
        const string tokenPrefix = "Token ";
        if (!authHeader.StartsWith(tokenPrefix, StringComparison.OrdinalIgnoreCase))
        {
            // Not our scheme — let other handlers (e.g. Bearer) try
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // 3. Extract the JWT string
        var jwtString = authHeader[tokenPrefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(jwtString))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing token value."));
        }

        // 4. Validate the JWT
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = GetTokenValidationParameters();

            var principal = tokenHandler.ValidateToken(jwtString, validationParameters, out var validatedToken);

            // Ensure the token is a JWT (not a different type of SecurityToken)
            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                _logger.LogWarning("Token scheme mismatch: received non-JWT token");
                return Task.FromResult(AuthenticateResult.Fail("Invalid token format."));
            }

            // 5. Assert required claims exist
            var userId = principal.FindFirst("user_id")?.Value;
            var username = principal.FindFirst(ClaimTypes.Name)?.Value
                           ?? principal.FindFirst("username")?.Value;
            if (userId is null || username is null)
            {
                _logger.LogWarning("Token validated but missing user_id or username claims");
                return Task.FromResult(AuthenticateResult.Fail("Token does not contain required claims."));
            }

            _logger.LogDebug("Token authentication succeeded for user {Username} (id={UserId})", username, userId);

            // 6. Create authentication ticket
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "Token authentication failed: token expired");
            return Task.FromResult(AuthenticateResult.Fail("Token has expired."));
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token authentication failed: invalid token");
            return Task.FromResult(AuthenticateResult.Fail("Invalid token."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token authentication failed with unexpected error");
            return Task.FromResult(AuthenticateResult.Fail("Token validation failed."));
        }
    }

    /// <summary>
    /// Builds the <see cref="TokenValidationParameters"/> using the same
    /// configuration as the JWT Bearer handler to ensure compatibility.
    /// </summary>
    private TokenValidationParameters GetTokenValidationParameters()
    {
        return new TokenValidationParameters
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
    }
}
