using System.Text.Json.Serialization;

namespace Paperless.Api.Dto.Requests;

/// <summary>
/// Request body for POST /api/token/ — login by username and password.
/// </summary>
public class LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Request body for POST /api/token/refresh/ — refresh an existing token.
/// </summary>
public class RefreshTokenRequest
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Request body for POST /api/token/verify/ — verify a token's validity.
/// </summary>
public class VerifyTokenRequest
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
