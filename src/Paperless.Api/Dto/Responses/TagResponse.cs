using System.Text.Json.Serialization;
using Paperless.Core.Documents.Entities;

namespace Paperless.Api.Dto.Responses;

/// <summary>
/// DRF-compatible tag response DTO.
/// Maps to the TagSerializer from the original paperless-ngx API.
/// </summary>
public class TagResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("slug")]
    public string? Slug { get; init; }

    [JsonPropertyName("color")]
    public string? Color { get; init; }

    [JsonPropertyName("text_color")]
    public string? TextColor { get; init; }

    [JsonPropertyName("match")]
    public string? Match { get; init; }

    [JsonPropertyName("matching_algorithm")]
    public int MatchingAlgorithm { get; init; }

    [JsonPropertyName("is_insensitive")]
    public bool IsInsensitive { get; init; }

    [JsonPropertyName("is_inbox_tag")]
    public bool IsInboxTag { get; init; }

    [JsonPropertyName("document_count")]
    public int DocumentCount { get; init; }

    [JsonPropertyName("owner")]
    public int? OwnerId { get; init; }

    [JsonPropertyName("user_can_change")]
    public bool UserCanChange { get; init; } = true;

    /// <summary>
    /// Maps a <see cref="Tag"/> entity to this response DTO.
    /// </summary>
    public static TagResponse FromEntity(Tag tag) => new()
    {
        Id = tag.Id,
        Name = tag.Name,
        Slug = tag.Slug,
        Color = tag.Color,
        TextColor = tag.TextColor,
        Match = tag.Match,
        MatchingAlgorithm = (int)tag.MatchingAlgorithm,
        IsInsensitive = tag.IsInsensitive,
        IsInboxTag = tag.IsInboxTag,
        DocumentCount = tag.Documents?.Count ?? 0,
        UserCanChange = true
    };
}
