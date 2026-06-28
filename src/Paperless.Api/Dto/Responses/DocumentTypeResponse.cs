using System.Text.Json.Serialization;
using Paperless.Core.Documents.Entities;

namespace Paperless.Api.Dto.Responses;

/// <summary>
/// DRF-compatible document type response DTO.
/// Maps to the DocumentTypeSerializer from the original paperless-ngx API.
/// </summary>
public class DocumentTypeResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("slug")]
    public string? Slug { get; init; }

    [JsonPropertyName("match")]
    public string? Match { get; init; }

    [JsonPropertyName("matching_algorithm")]
    public int MatchingAlgorithm { get; init; }

    [JsonPropertyName("is_insensitive")]
    public bool IsInsensitive { get; init; }

    [JsonPropertyName("document_count")]
    public int DocumentCount { get; init; }

    [JsonPropertyName("owner")]
    public int? OwnerId { get; init; }

    [JsonPropertyName("user_can_change")]
    public bool UserCanChange { get; init; } = true;

    /// <summary>
    /// Maps a <see cref="DocumentType"/> entity to this response DTO.
    /// </summary>
    public static DocumentTypeResponse FromEntity(DocumentType documentType) => new()
    {
        Id = documentType.Id,
        Name = documentType.Name,
        Slug = documentType.Slug,
        Match = documentType.Match,
        MatchingAlgorithm = (int)documentType.MatchingAlgorithm,
        IsInsensitive = documentType.IsInsensitive,
        DocumentCount = documentType.Documents?.Count ?? 0,
        UserCanChange = true
    };
}
