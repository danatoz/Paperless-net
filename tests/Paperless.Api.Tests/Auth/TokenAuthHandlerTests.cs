using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Paperless.Api.Auth;

namespace Paperless.Api.Tests.Auth;

public class TokenAuthHandlerTests
{
    private readonly JwtOptions _jwtOptions;
    private readonly IServiceProvider _serviceProvider;

    public TokenAuthHandlerTests()
    {
        _jwtOptions = new JwtOptions
        {
            Key = "This-Is-A-Test-Key-For-Testing-Purposes-Only-1234567890!",
            Issuer = "Paperless-Test",
            Audience = "Paperless-Test-SPA",
            ExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(_jwtOptions));
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithValidTokenHeader_ReturnsSuccess()
    {
        // Arrange
        var token = GenerateValidJwt("testuser", 1, ["User"]);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        httpContext.Request.Headers["Authorization"] = $"Token {token}";

        var handler = CreateHandler(httpContext);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Equal("testuser", result.Principal.FindFirst(ClaimTypes.Name)?.Value);
        Assert.Equal("1", result.Principal.FindFirst("user_id")?.Value);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithBearerHeader_ReturnsNoResult()
    {
        // Arrange
        var token = GenerateValidJwt("testuser", 1, ["User"]);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        httpContext.Request.Headers["Authorization"] = $"Bearer {token}";

        var handler = CreateHandler(httpContext);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Ticket);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithMissingHeader_ReturnsNoResult()
    {
        // Arrange
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };

        var handler = CreateHandler(httpContext);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Ticket);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithInvalidToken_ReturnsFail()
    {
        // Arrange
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        httpContext.Request.Headers["Authorization"] = "Token invalid-token-string";

        var handler = CreateHandler(httpContext);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithExpiredToken_ReturnsFail()
    {
        // Arrange
        var token = GenerateExpiredJwt("expired", 99, ["User"]);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        httpContext.Request.Headers["Authorization"] = $"Token {token}";

        var handler = CreateHandler(httpContext);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
        Assert.Contains("expired", result.Failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithEmptyTokenValue_ReturnsFail()
    {
        // Arrange
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        httpContext.Request.Headers["Authorization"] = "Token ";

        var handler = CreateHandler(httpContext);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithTokenFromDifferentIssuer_ReturnsFail()
    {
        // Arrange
        var differentKey = "Different-Key-That-Is-Still-32-Bytes-Long-For-HmacSha256!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(differentKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "Different-Issuer",
            audience: _jwtOptions.Audience,
            claims: [new Claim("user_id", "1"), new Claim(ClaimTypes.Name, "hacker")],
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );
        var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
            .WriteToken(token);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        httpContext.Request.Headers["Authorization"] = $"Token {tokenString}";

        var handler = CreateHandler(httpContext);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
    }

    // ── Helpers ───────────────────────────────────────────────────

    private TokenAuthHandler CreateHandler(HttpContext httpContext)
    {
        var optionsMonitor = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
        optionsMonitor.Get("Token").Returns(new AuthenticationSchemeOptions());

        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        var handler = new TokenAuthHandler(
            optionsMonitor,
            loggerFactory,
            UrlEncoder.Default,
            Options.Create(_jwtOptions)
        );

        // Wire up the handler with the scheme and context
        var scheme = new AuthenticationScheme("Token", "Token", typeof(TokenAuthHandler));
        handler.InitializeAsync(scheme, httpContext).GetAwaiter().GetResult();

        return handler;
    }

    private string GenerateValidJwt(string username, int userId, string[] roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("user_id", userId.ToString()),
            new(ClaimTypes.Name, username),
            new("username", username)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateExpiredJwt(string username, int userId, string[] roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("user_id", userId.ToString()),
            new(ClaimTypes.Name, username),
            new("username", username)
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(-30), // expired
            signingCredentials: credentials
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
