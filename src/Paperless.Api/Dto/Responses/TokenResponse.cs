using System.Text.Json.Serialization;

namespace Paperless.Api.Dto.Responses;

/// <summary>
/// Response body for POST /api/token/ and POST /api/token/refresh/.
/// Compatible with the Angular SPA expected format { token }.
/// </summary>
public class TokenResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    public TokenResponse()
    {
    }

    public TokenResponse(string token)
    {
        Token = token;
    }
}

/// <summary>
/// Response body for POST /api/token/verify/.
/// </summary>
public class TokenVerifyResponse
{
    [JsonPropertyName("valid")]
    public bool Valid { get; set; }
}
