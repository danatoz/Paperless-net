using System.Text.Json.Serialization;

namespace Paperless.Api.Dto.Requests;

/// <summary>
/// Request body for POST /api/documents/merge/.
/// </summary>
public class MergeRequest
{
    [JsonPropertyName("documents")]
    public IReadOnlyCollection<int> DocumentIds { get; init; } = Array.Empty<int>();
}
