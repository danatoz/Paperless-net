using System.Text.Json.Serialization;

namespace Paperless.Api.Dto.Requests;

/// <summary>
/// Request body for PUT /api/documents/{id}/ (full update).
/// </summary>
public class UpdateDocumentRequest
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("correspondent")]
    public int? CorrespondentId { get; init; }

    [JsonPropertyName("document_type")]
    public int? DocumentTypeId { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyCollection<int> TagIds { get; init; } = Array.Empty<int>();

    [JsonPropertyName("created")]
    public DateTime? Created { get; init; }

    [JsonPropertyName("archive_serial_number")]
    public int? ArchiveSerialNumber { get; init; }
}
