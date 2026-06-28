using System.Text.Json.Serialization;

namespace Paperless.Api.Dto.Responses;

/// <summary>
/// DRF-compatible paginated response.
/// Matches the format returned by DRF's PageNumberPagination:
/// <code>
/// {
///   "count": 123,
///   "next": "http://.../?page=4",
///   "previous": "http://.../?page=2",
///   "all": [1, 2, 3, ...],
///   "results": [...]
/// }
/// </code>
/// </summary>
/// <typeparam name="T">The type of items in the results array.</typeparam>
public class PaginatedResponse<T>
{
    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; init; }

    /// <summary>
    /// URL to the next page, or null if on the last page.
    /// </summary>
    [JsonPropertyName("next")]
    public string? Next { get; init; }

    /// <summary>
    /// URL to the previous page, or null if on the first page.
    /// </summary>
    [JsonPropertyName("previous")]
    public string? Previous { get; init; }

    /// <summary>
    /// Array of all matching item IDs (not just current page).
    /// Used by the frontend for bulk operations.
    /// </summary>
    [JsonPropertyName("all")]
    public IReadOnlyCollection<int> All { get; init; } = Array.Empty<int>();

    /// <summary>
    /// The items on the current page.
    /// </summary>
    [JsonPropertyName("results")]
    public IReadOnlyCollection<T> Results { get; init; } = Array.Empty<T>();
}
