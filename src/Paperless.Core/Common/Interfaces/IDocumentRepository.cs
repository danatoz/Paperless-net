using Paperless.Core.Common.Models;
using Paperless.Core.Common.Specifications;
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
    /// Retrieves a paginated, filtered, and sorted list of documents
    /// using a specification.
    /// </summary>
    Task<PagedResult<Document>> GetAllAsync(ISpecification<Document> spec, CancellationToken ct = default);

    /// <summary>
    /// Searches documents by title, content, or metadata.
    /// </summary>
    Task<IReadOnlyCollection<Document>> SearchAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Searches documents by content with pagination and sorting via specification.
    /// </summary>
    Task<PagedResult<Document>> SearchAsync(string query, ISpecification<Document> spec, CancellationToken ct = default);

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

    /// <summary>
    /// Performs a bulk update on documents matching the given specification.
    /// Uses EF Core ExecuteUpdate for efficiency.
    /// </summary>
    Task<int> BulkUpdateAsync(ISpecification<Document> spec, Action<Document> updateAction, CancellationToken ct = default);

    /// <summary>
    /// Performs a bulk soft-delete on documents matching the given specification.
    /// Uses EF Core ExecuteDelete for efficiency.
    /// </summary>
    Task<int> BulkDeleteAsync(ISpecification<Document> spec, CancellationToken ct = default);
}
