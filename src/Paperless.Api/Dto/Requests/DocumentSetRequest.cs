using System.Text.Json.Serialization;

namespace Paperless.Api.Dto.Requests;

/// <summary>
/// Base request for bulk operations that operate on a set of document IDs.
/// Used by delete, reprocess, merge endpoints.
/// </summary>
public class DocumentSetRequest
{
    [JsonPropertyName("documents")]
    public IReadOnlyCollection<int> DocumentIds { get; init; } = Array.Empty<int>();
}
