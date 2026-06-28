namespace Paperless.Api.Auth;

/// <summary>
/// Options bound from the "Jwt" configuration section.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Symmetric signing key for JWT token signing and validation.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer (iss claim).
    /// </summary>
    public string Issuer { get; set; } = "Paperless";

    /// <summary>
    /// Token audience (aud claim).
    /// </summary>
    public string Audience { get; set; } = "Paperless-SPA";

    /// <summary>
    /// Access token lifetime in minutes.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token lifetime in days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
