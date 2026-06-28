namespace Paperless.Infrastructure.Search;

/// <summary>
/// Represents a paginated search result set from the Lucene search backend.
/// Contains the matched document IDs along with pagination metadata.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// The document IDs matching the search query on the current page.
    /// </summary>
    public IReadOnlyCollection<int> DocumentIds { get; init; } = Array.Empty<int>();

    /// <summary>
    /// The total number of documents matching the query (across all pages).
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// The number of results per page.
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// The total number of pages based on TotalCount and PageSize.
    /// </summary>
    public int TotalPages => TotalCount > 0
        ? (int)Math.Ceiling(TotalCount / (double)PageSize)
        : 0;

    /// <summary>
    /// Whether there is a next page available.
    /// </summary>
    public bool HasNext => Page < TotalPages;

    /// <summary>
    /// Whether there is a previous page available.
    /// </summary>
    public bool HasPrevious => Page > 1;
}
