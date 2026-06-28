using System.Text.Json.Serialization;

namespace Paperless.Api.Dto.Requests;

/// <summary>
/// Request body for PATCH /api/documents/{id}/ (partial update).
/// All fields are optional — only provided fields will be updated.
/// </summary>
public class PatchDocumentRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("correspondent")]
    public int? CorrespondentId { get; init; }

    [JsonPropertyName("document_type")]
    public int? DocumentTypeId { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyCollection<int>? TagIds { get; init; }

    [JsonPropertyName("created")]
    public DateTime? Created { get; init; }

    [JsonPropertyName("archive_serial_number")]
    public int? ArchiveSerialNumber { get; init; }
}
