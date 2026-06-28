using Microsoft.Extensions.Options;
using Paperless.Shared.Abstractions;

namespace Paperless.Api.Auth;

/// <summary>
/// Default implementation of <see cref="IAuthService"/>.
///
/// Uses config-based default credentials for development (M2-08 will
/// integrate with the actual user store / ASP.NET Core Identity).
///
/// Default users (configured in "DefaultUsers" section):
/// - admin / admin (role: Admin)
/// - user / user   (role: User)
/// </summary>
public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly List<StoredUser> _users;

    public AuthService(
        IOptions<DefaultUsersOptions> defaultUsersOptions,
        ILogger<AuthService> logger)
    {
        _logger = logger;

        var configuredUsers = defaultUsersOptions.Value.Users;
        if (configuredUsers is { Count: > 0 })
        {
            _users = configuredUsers
                .Select(u => new StoredUser
                {
                    Id = u.Id,
                    Username = u.Username,
                    Password = u.Password,
                    Roles = u.Roles
                })
                .ToList();
        }
        else
        {
            _users = new List<StoredUser>
            {
                new() { Id = 1, Username = "admin", Password = "admin", Roles = ["Admin", "User"] },
                new() { Id = 2, Username = "user", Password = "user", Roles = ["User"] }
            };
        }

        _logger.LogInformation("AuthService initialized with {UserCount} default user(s)", _users.Count);
    }

    public Task<Result<AuthenticatedUser>> ValidateCredentialsAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult<Result<AuthenticatedUser>>(
                new Error("Auth.InvalidCredentials", "Username and password are required."));
        }

        var user = _users.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) &&
            u.Password == password);

        if (user is null)
        {
            _logger.LogWarning("Failed login attempt for username: {Username}", username);
            return Task.FromResult<Result<AuthenticatedUser>>(
                new Error("Auth.InvalidCredentials", "Invalid username or password."));
        }

        _logger.LogInformation("User {Username} authenticated successfully", username);

        return Task.FromResult<Result<AuthenticatedUser>>(
            new AuthenticatedUser
            {
                Id = user.Id,
                Username = user.Username,
                Roles = user.Roles.ToArray()
            });
    }

    public Task<Result<AuthenticatedUser>> GetUserByIdAsync(int userId)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);

        if (user is null)
        {
            return Task.FromResult<Result<AuthenticatedUser>>(
                new Error("Auth.UserNotFound", $"User with ID {userId} not found."));
        }

        return Task.FromResult<Result<AuthenticatedUser>>(
            new AuthenticatedUser
            {
                Id = user.Id,
                Username = user.Username,
                Roles = user.Roles.ToArray()
            });
    }

    private class StoredUser
    {
        public int Id { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string[] Roles { get; init; } = Array.Empty<string>();
    }
}

/// <summary>
/// Configuration section for default development users.
/// </summary>
public class DefaultUsersOptions
{
    public const string SectionName = "DefaultUsers";

    public List<DefaultUserEntry> Users { get; set; } = new();
}

/// <summary>
/// A single default user entry from configuration.
/// </summary>
public class DefaultUserEntry
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
}
