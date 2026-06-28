using System.Text.Json.Serialization;

namespace Paperless.Api.Dto.Requests;

/// <summary>
/// Request body for POST /api/documents/remove_password/.
/// </summary>
public class RemovePasswordRequest
{
    [JsonPropertyName("documents")]
    public IReadOnlyCollection<int> DocumentIds { get; init; } = Array.Empty<int>();

    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;
}
