using System.Text.Json.Serialization;

namespace Paperless.Api.Dto.Requests;

/// <summary>
/// Request body for POST /api/documents/bulk_download/.
/// </summary>
public class BulkDownloadRequest
{
    [JsonPropertyName("documents")]
    public IReadOnlyCollection<int> DocumentIds { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Content type to download: "both" (default), "originals", or "archive".
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; init; } = "both";
}
