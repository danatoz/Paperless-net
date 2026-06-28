using System.Text.Json.Serialization;

namespace Paperless.Api.Dto.Requests;

/// <summary>
/// Request body for POST /api/documents/rotate/.
/// </summary>
public class RotateRequest
{
    [JsonPropertyName("documents")]
    public IReadOnlyCollection<int> DocumentIds { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Rotation angle in degrees. Typically 90, 180, or 270.
    /// </summary>
    [JsonPropertyName("rotation")]
    public int Rotation { get; init; } = 90;
}
