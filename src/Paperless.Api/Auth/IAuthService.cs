using Paperless.Shared.Abstractions;

namespace Paperless.Api.Auth;

/// <summary>
/// Service for authenticating users and managing credentials.
/// Maps to DRF's authentication backend (ModelBackend / TokenBackend).
/// Full user management integration is in M2-08 (UsersController).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Validates the given username and password, and returns authenticated user info.
    /// Returns a failed result if credentials are invalid.
    /// </summary>
    Task<Result<AuthenticatedUser>> ValidateCredentialsAsync(string username, string password);

    /// <summary>
    /// Returns the authenticated user info for a given user ID.
    /// </summary>
    Task<Result<AuthenticatedUser>> GetUserByIdAsync(int userId);
}

/// <summary>
/// Represents an authenticated user with identity and roles.
/// </summary>
public class AuthenticatedUser
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}
