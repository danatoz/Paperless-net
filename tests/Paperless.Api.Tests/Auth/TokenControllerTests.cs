using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Paperless.Api.Auth;
using Paperless.Api.Dto.Requests;
using Paperless.Api.Dto.Responses;
using Paperless.Shared.Abstractions;

namespace Paperless.Api.Tests.Auth;

public class TokenControllerTests
{
    private readonly IAuthService _authService;
    private readonly IOptions<JwtOptions> _jwtOptions;
    private readonly TokenController _controller;

    public TokenControllerTests()
    {
        _authService = Substitute.For<IAuthService>();
        _jwtOptions = Options.Create(new JwtOptions
        {
            Key = "This-Is-A-Test-Key-For-Testing-Purposes-Only-1234567890!",
            Issuer = "Paperless-Test",
            Audience = "Paperless-Test-SPA",
            ExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        });
        var logger = Substitute.For<ILogger<TokenController>>();

        _controller = new TokenController(_authService, _jwtOptions, logger);
    }

    // ── POST /api/token/ (Login) ──────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithToken()
    {
        // Arrange
        var request = new LoginRequest { Username = "admin", Password = "admin" };
        var authUser = new AuthenticatedUser
        {
            Id = 1,
            Username = "admin",
            Roles = ["Admin", "User"]
        };

        _authService.ValidateCredentialsAsync("admin", "admin")
            .Returns(Task.FromResult<Result<AuthenticatedUser>>(authUser));

        // Act
        var result = await _controller.LoginAsync(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TokenResponse>(okResult.Value);
        Assert.NotNull(response.Token);
        Assert.NotEmpty(response.Token);

        // Verify the token is a valid JWT
        var tokenParts = response.Token.Split('.');
        Assert.Equal(3, tokenParts.Length);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        // Arrange
        var request = new LoginRequest { Username = "admin", Password = "wrong" };

        _authService.ValidateCredentialsAsync("admin", "wrong")
            .Returns(Task.FromResult<Result<AuthenticatedUser>>(
                new Error("Auth.InvalidCredentials", "Invalid username or password.")));

        // Act
        var result = await _controller.LoginAsync(request, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task Login_WithEmptyCredentials_Returns401()
    {
        // Arrange
        var request = new LoginRequest { Username = "", Password = "" };

        _authService.ValidateCredentialsAsync("", "")
            .Returns(Task.FromResult<Result<AuthenticatedUser>>(
                new Error("Auth.InvalidCredentials", "Username and password are required.")));

        // Act
        var result = await _controller.LoginAsync(request, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ── POST /api/token/refresh/ ──────────────────────────────────

    [Fact]
    public async Task Refresh_WithValidToken_Returns200WithNewToken()
    {
        // Arrange
        var authUser = new AuthenticatedUser { Id = 1, Username = "admin", Roles = ["Admin"] };
        var token = GenerateTestToken(authUser);

        _authService.GetUserByIdAsync(1)
            .Returns(Task.FromResult<Result<AuthenticatedUser>>(authUser));

        var request = new RefreshTokenRequest { Token = token };

        // Act
        var result = await _controller.RefreshAsync(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TokenResponse>(okResult.Value);
        Assert.NotNull(response.Token);
        Assert.NotEmpty(response.Token);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_Returns401()
    {
        // Arrange
        var token = GenerateExpiredTestToken();

        var request = new RefreshTokenRequest { Token = token };

        // Act
        var result = await _controller.RefreshAsync(request, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_Returns401()
    {
        // Arrange
        var request = new RefreshTokenRequest { Token = "invalid-token-string" };

        // Act
        var result = await _controller.RefreshAsync(request, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ── POST /api/token/verify/ ───────────────────────────────────

    [Fact]
    public void Verify_WithValidToken_Returns200WithValidTrue()
    {
        // Arrange
        var authUser = new AuthenticatedUser { Id = 1, Username = "admin", Roles = ["Admin"] };
        var token = GenerateTestToken(authUser);
        var request = new VerifyTokenRequest { Token = token };

        // Act
        var result = _controller.Verify(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TokenVerifyResponse>(okResult.Value);
        Assert.True(response.Valid);
    }

    [Fact]
    public void Verify_WithInvalidToken_Returns401WithValidFalse()
    {
        // Arrange
        var request = new VerifyTokenRequest { Token = "invalid-token-string" };

        // Act
        var result = _controller.Verify(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<TokenVerifyResponse>(unauthorizedResult.Value);
        Assert.False(response.Valid);
    }

    [Fact]
    public void Verify_WithExpiredToken_Returns401WithValidFalse()
    {
        // Arrange
        var token = GenerateExpiredTestToken();
        var request = new VerifyTokenRequest { Token = token };

        // Act
        var result = _controller.Verify(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<TokenVerifyResponse>(unauthorizedResult.Value);
        Assert.False(response.Valid);
    }

    // ── Helpers ───────────────────────────────────────────────────

    private string GenerateTestToken(AuthenticatedUser user)
    {
        var options = _jwtOptions.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("user_id", user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("username", user.Username),
            new(ClaimTypes.Role, string.Join(",", user.Roles))
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateExpiredTestToken()
    {
        var options = _jwtOptions.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("user_id", "999"),
            new(ClaimTypes.Name, "expired-user"),
            new("username", "expired-user")
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(-30),  // expired 30 min ago
            signingCredentials: credentials
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
