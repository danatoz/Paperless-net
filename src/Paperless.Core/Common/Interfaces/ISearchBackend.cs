using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Common.Interfaces;

/// <summary>
/// Search backend interface for full-text search operations.
/// Abstracts the underlying search engine (Lucene.NET, Tantivy, etc.).
/// Maps to document search operations from the original paperless-ngx.
/// </summary>
public interface ISearchBackend
{
    /// <summary>
    /// Indexes a document for full-text search.
    /// </summary>
    Task IndexDocumentAsync(Document document, CancellationToken ct = default);

    /// <summary>
    /// Removes a document from the search index.
    /// </summary>
    Task RemoveFromIndexAsync(int documentId, CancellationToken ct = default);

    /// <summary>
    /// Performs a full-text search and returns matching document IDs with relevance scores.
    /// </summary>
    Task<IReadOnlyCollection<int>> SearchAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Performs an autocomplete search returning suggested terms.
    /// </summary>
    Task<IReadOnlyCollection<string>> AutocompleteAsync(string prefix, CancellationToken ct = default);
}
