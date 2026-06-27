namespace Paperless.Core.Common.Interfaces;

/// <summary>
/// Search index lifecycle management interface.
/// Handles creation, opening, and deletion of the physical search index.
/// </summary>
public interface ISearchIndex
{
    /// <summary>
    /// Creates a new search index.
    /// </summary>
    Task CreateIndexAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Opens an existing search index for reading/writing.
    /// </summary>
    Task OpenIndexAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Deletes a search index.
    /// </summary>
    Task DeleteIndexAsync(string name, CancellationToken ct = default);
}
