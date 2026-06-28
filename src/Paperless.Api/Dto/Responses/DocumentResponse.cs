using System.Text.Json.Serialization;

namespace Paperless.Api.Dto.Responses;

/// <summary>
/// DRF-compatible document response DTO.
/// Maps to the DocumentSerializer from the original paperless-ngx API.
/// </summary>
public class DocumentResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("correspondent")]
    public int? CorrespondentId { get; init; }

    [JsonPropertyName("document_type")]
    public int? DocumentTypeId { get; init; }

    [JsonPropertyName("storage_path")]
    public int? StoragePathId { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyCollection<int> Tags { get; init; } = Array.Empty<int>();

    [JsonPropertyName("custom_fields")]
    public IReadOnlyCollection<DocumentCustomFieldResponse> CustomFields { get; init; } = Array.Empty<DocumentCustomFieldResponse>();

    [JsonPropertyName("created")]
    public DateTime? Created { get; init; }

    [JsonPropertyName("created_date")]
    public string? CreatedDate { get; init; }

    [JsonPropertyName("modified")]
    public DateTime Modified { get; init; }

    [JsonPropertyName("added")]
    public DateTime Added { get; init; }

    [JsonPropertyName("archive_serial_number")]
    public int? ArchiveSerialNumber { get; init; }

    [JsonPropertyName("original_file_name")]
    public string? OriginalFileName { get; init; }

    [JsonPropertyName("archived_file_name")]
    public string? ArchivedFileName { get; init; }

    [JsonPropertyName("owner")]
    public int? OwnerId { get; init; }

    [JsonPropertyName("is_shared_by_requester")]
    public bool IsSharedByRequester { get; init; }

    [JsonPropertyName("notes")]
    public IReadOnlyCollection<DocumentNoteResponse> Notes { get; init; } = Array.Empty<DocumentNoteResponse>();

    /// <summary>
    /// Maps a <see cref="Core.Documents.Entities.Document"/> entity to this response DTO.
    /// </summary>
    public static DocumentResponse FromEntity(Core.Documents.Entities.Document doc) => new()
    {
        Id = doc.Id,
        CorrespondentId = doc.CorrespondentId,
        DocumentTypeId = doc.DocumentTypeId,
        Title = doc.Title,
        Content = doc.Content,
        Tags = doc.Tags.Select(t => t.Id).ToList().AsReadOnly(),
        CustomFields = doc.CustomFields
            .Select(cf => new DocumentCustomFieldResponse
            {
                Field = cf.CustomFieldId,
                Value = cf.Value
            })
            .ToList()
            .AsReadOnly(),
        Created = doc.Created,
        CreatedDate = doc.Created?.ToString("yyyy-MM-dd"),
        Modified = doc.Modified,
        Added = doc.Added,
        ArchiveSerialNumber = doc.ArchiveSerialNumber,
        OriginalFileName = doc.Filename,
        OwnerId = doc.OwnerId,
        IsSharedByRequester = false,
        Notes = Array.Empty<DocumentNoteResponse>()
    };
}

/// <summary>
/// Represents a custom field value on a document.
/// </summary>
public class DocumentCustomFieldResponse
{
    [JsonPropertyName("field")]
    public int Field { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }
}

/// <summary>
/// Represents a note attached to a document.
/// </summary>
public class DocumentNoteResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("note")]
    public string? Note { get; init; }

    [JsonPropertyName("created")]
    public DateTime Created { get; init; }

    [JsonPropertyName("user")]
    public int? UserId { get; init; }
}
