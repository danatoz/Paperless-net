using System.Text.Json.Serialization;

namespace Paperless.Api.Dto.Requests;

/// <summary>
/// Request body for POST /api/document_types/.
/// </summary>
public class CreateDocumentTypeRequest
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("match")]
    public string? Match { get; init; }

    [JsonPropertyName("matching_algorithm")]
    public int? MatchingAlgorithm { get; init; }

    [JsonPropertyName("is_insensitive")]
    public bool? IsInsensitive { get; init; }
}

/// <summary>
/// Request body for PUT /api/document_types/{id}/ (full update).
/// </summary>
public class UpdateDocumentTypeRequest
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("match")]
    public string? Match { get; init; }

    [JsonPropertyName("matching_algorithm")]
    public int? MatchingAlgorithm { get; init; }

    [JsonPropertyName("is_insensitive")]
    public bool? IsInsensitive { get; init; }
}

/// <summary>
/// Request body for PATCH /api/document_types/{id}/ (partial update).
/// All fields are optional — only provided fields will be updated.
/// </summary>
public class PatchDocumentTypeRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("match")]
    public string? Match { get; init; }

    [JsonPropertyName("matching_algorithm")]
    public int? MatchingAlgorithm { get; init; }

    [JsonPropertyName("is_insensitive")]
    public bool? IsInsensitive { get; init; }
}

/// <summary>
/// Query parameters for GET /api/document_types/.
/// </summary>
public class DocumentTypeFilterRequest
{
    /// <summary>
    /// Page number (1-based). Defaults to 1.
    /// </summary>
    public int? Page { get; init; } = 1;

    /// <summary>
    /// Number of items per page. Defaults to 20.
    /// </summary>
    public int? PageSize { get; init; } = 20;

    /// <summary>
    /// Search query — filters by name (case-insensitive contains).
    /// </summary>
    public string? Query { get; init; }

    /// <summary>
    /// Comma-separated ordering field(s). Prefix with '-' for descending.
    /// Supported: "name".
    /// </summary>
    public string? Ordering { get; init; }
}
