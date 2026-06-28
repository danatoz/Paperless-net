using System.Text.Json.Serialization;
using Paperless.Core.Documents.Entities;

namespace Paperless.Api.Dto.Responses;

/// <summary>
/// DRF-compatible correspondent response DTO.
/// Maps to the CorrespondentSerializer from the original paperless-ngx API.
/// </summary>
public class CorrespondentResponse
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
    /// Maps a <see cref="Correspondent"/> entity to this response DTO.
    /// </summary>
    public static CorrespondentResponse FromEntity(Correspondent correspondent) => new()
    {
        Id = correspondent.Id,
        Name = correspondent.Name,
        Slug = correspondent.Slug,
        Match = correspondent.Match,
        MatchingAlgorithm = (int)correspondent.MatchingAlgorithm,
        IsInsensitive = correspondent.IsInsensitive,
        DocumentCount = correspondent.Documents?.Count ?? 0,
        UserCanChange = true
    };
}
