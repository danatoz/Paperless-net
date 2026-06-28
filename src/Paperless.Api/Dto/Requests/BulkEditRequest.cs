using System.Text.Json.Serialization;

namespace Paperless.Api.Dto.Requests;

/// <summary>
/// Request body for POST /api/documents/bulk_edit/.
/// Matches the DRF bulk edit format from the original paperless-ngx.
/// </summary>
public class BulkEditRequest
{
    [JsonPropertyName("documents")]
    public IReadOnlyCollection<int> DocumentIds { get; init; } = Array.Empty<int>();

    /// <summary>
    /// The bulk edit method to apply.
    /// Examples: "set_correspondent", "set_document_type", "add_tag", "remove_tag", "modify_tags", "set_permissions".
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; init; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, object?>? Parameters { get; init; }
}
