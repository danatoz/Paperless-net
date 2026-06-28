using Paperless.Core.Common.Models;
using Paperless.Core.Common.Specifications;
using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Common.Interfaces;

/// <summary>
/// Repository interface for DocumentType entity operations.
/// </summary>
public interface IDocumentTypeRepository
{
    /// <summary>
    /// Gets a document type by its unique identifier.
    /// </summary>
    Task<DocumentType?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a paginated, filtered, and sorted list of document types
    /// using a specification.
    /// </summary>
    Task<PagedResult<DocumentType>> GetAllAsync(ISpecification<DocumentType> spec, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all document types.
    /// </summary>
    Task<IReadOnlyCollection<DocumentType>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Adds a new document type.
    /// </summary>
    Task<DocumentType> AddAsync(DocumentType documentType, CancellationToken ct = default);

    /// <summary>
    /// Marks an existing document type as updated.
    /// </summary>
    void Update(DocumentType documentType);

    /// <summary>
    /// Soft-deletes a document type.
    /// </summary>
    void Delete(DocumentType documentType);
}
