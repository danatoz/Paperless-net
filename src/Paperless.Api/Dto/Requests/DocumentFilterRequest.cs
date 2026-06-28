namespace Paperless.Api.Dto.Requests;

/// <summary>
/// Query parameters for the GET /api/documents/ endpoint.
/// Supports filtering, pagination, search, and sorting.
/// </summary>
public class DocumentFilterRequest
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
    /// Full-text search query (searches title + content).
    /// </summary>
    public string? Query { get; init; }

    /// <summary>
    /// Filter by correspondent ID.
    /// </summary>
    public int? Correspondent { get; init; }

    /// <summary>
    /// Filter by document type ID.
    /// </summary>
    public int? DocumentType { get; init; }

    /// <summary>
    /// Filter by tag IDs (comma-separated). AND semantics.
    /// </summary>
    public string? Tags { get; init; }

    /// <summary>
    /// Filter by created date greater than or equal (ISO 8601).
    /// </summary>
    public DateTime? CreatedAfter { get; init; }

    /// <summary>
    /// Filter by created date less than or equal (ISO 8601).
    /// </summary>
    public DateTime? CreatedBefore { get; init; }

    /// <summary>
    /// Filter by added date greater than or equal (ISO 8601).
    /// </summary>
    public DateTime? AddedAfter { get; init; }

    /// <summary>
    /// Filter by added date less than or equal (ISO 8601).
    /// </summary>
    public DateTime? AddedBefore { get; init; }

    /// <summary>
    /// Comma-separated ordering field(s). Prefix with '-' for descending.
    /// Supported: "created", "added", "title", "correspondent", "document_type".
    /// Example: "-created,title" sorts by created descending, then title ascending.
    /// </summary>
    public string? Ordering { get; init; }

    /// <summary>
    /// When true, include trashed (soft-deleted) documents.
    /// </summary>
    public bool? Trash { get; init; }

    /// <summary>
    /// Parses the Tags query parameter into a list of integers.
    /// Tags can be specified as "1,2,3" or "1" or "1, 2, 3".
    /// </summary>
    public IReadOnlyCollection<int>? ParseTagIds()
    {
        if (string.IsNullOrWhiteSpace(Tags))
            return null;

        return Tags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList()
            .AsReadOnly();
    }
}
