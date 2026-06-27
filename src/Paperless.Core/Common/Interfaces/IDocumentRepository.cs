using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Common.Interfaces;

/// <summary>
/// Repository interface for Document entity operations.
/// Maps to document queries from the original paperless-ngx views and consumer.
/// </summary>
public interface IDocumentRepository
{
    /// <summary>
    /// Gets a document by its unique identifier.
    /// </summary>
    Task<Document?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all documents (optionally with filtering/sorting).
    /// </summary>
    Task<IReadOnlyCollection<Document>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Searches documents by title, content, or metadata.
    /// </summary>
    Task<IReadOnlyCollection<Document>> SearchAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Adds a new document to the repository.
    /// </summary>
    Task<Document> AddAsync(Document document, CancellationToken ct = default);

    /// <summary>
    /// Marks an existing document as updated in the change tracker.
    /// </summary>
    void Update(Document document);

    /// <summary>
    /// Soft-deletes a document (sets IsDeleted flag).
    /// </summary>
    void Delete(Document document);

    /// <summary>
    /// Gets documents associated with a specific correspondent.
    /// </summary>
    Task<IReadOnlyCollection<Document>> GetByCorrespondentAsync(int correspondentId, CancellationToken ct = default);

    /// <summary>
    /// Gets documents that have all the specified tags.
    /// </summary>
    Task<IReadOnlyCollection<Document>> GetByTagsAsync(IReadOnlyCollection<int> tagIds, CancellationToken ct = default);

    /// <summary>
    /// Finds a document by its SHA-256 checksum (for deduplication).
    /// </summary>
    Task<Document?> GetByChecksumAsync(string checksum, CancellationToken ct = default);
}
