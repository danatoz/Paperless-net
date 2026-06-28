namespace Paperless.Infrastructure.Search;

/// <summary>
/// Represents a full-text search query with optional filters and pagination.
/// Used by <see cref="LuceneSearchBackend"/> for advanced search operations.
/// </summary>
public class SearchQuery
{
    /// <summary>
    /// The free-text search query string.
    /// Supports Lucene query syntax (e.g., "term1 AND term2", "phrase search").
    /// </summary>
    public string Query { get; init; } = string.Empty;

    /// <summary>
    /// The page number for pagination (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// The number of results per page. Defaults to 20.
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// The field to sort by. Supported values: "created", "added", "title", "score".
    /// When null or empty, sorts by relevance score (descending).
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Whether to sort in ascending order. Defaults to true.
    /// Ignored when SortBy is null/empty (uses score descending).
    /// </summary>
    public bool SortAscending { get; init; } = true;

    // ── Filters ──────────────────────────────────────────────────

    /// <summary>
    /// Filter by tag IDs (AND semantics: document must have all specified tags).
    /// </summary>
    public IReadOnlyCollection<int>? TagIds { get; init; }

    /// <summary>
    /// Filter by correspondent ID.
    /// </summary>
    public int? CorrespondentId { get; init; }

    /// <summary>
    /// Filter by document type ID.
    /// </summary>
    public int? DocumentTypeId { get; init; }

    /// <summary>
    /// Filter by inbox status. When true, only inbox documents are returned.
    /// </summary>
    public bool? IsInbox { get; init; }

    /// <summary>
    /// Filter by storage path.
    /// </summary>
    public string? StoragePath { get; init; }

    /// <summary>
    /// Filter by owner ID.
    /// </summary>
    public int? OwnerId { get; init; }

    /// <summary>
    /// Filter by archive serial number.
    /// </summary>
    public int? ArchiveSerialNumber { get; init; }

    // ── Date range filters ───────────────────────────────────────

    /// <summary>
    /// Only return documents created on or after this date.
    /// </summary>
    public DateTime? CreatedAfter { get; init; }

    /// <summary>
    /// Only return documents created on or before this date.
    /// </summary>
    public DateTime? CreatedBefore { get; init; }

    /// <summary>
    /// Only return documents added on or after this date.
    /// </summary>
    public DateTime? AddedAfter { get; init; }

    /// <summary>
    /// Only return documents added on or before this date.
    /// </summary>
    public DateTime? AddedBefore { get; init; }

    /// <summary>
    /// Validates the query parameters and returns a description of any issues.
    /// </summary>
    public IReadOnlyCollection<string> Validate()
    {
        var errors = new List<string>();

        if (Page < 1)
            errors.Add("Page must be greater than or equal to 1.");

        if (PageSize < 1)
            errors.Add("PageSize must be greater than or equal to 1.");

        if (PageSize > 100000)
            errors.Add("PageSize must not exceed 100000.");

        return errors;
    }
}
