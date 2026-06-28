using Paperless.Core.Common.Models;
using Paperless.Core.Common.Specifications;
using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Common.Interfaces;

/// <summary>
/// Repository interface for Correspondent entity operations.
/// </summary>
public interface ICorrespondentRepository
{
    /// <summary>
    /// Gets a correspondent by its unique identifier.
    /// </summary>
    Task<Correspondent?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a paginated, filtered, and sorted list of correspondents
    /// using a specification.
    /// </summary>
    Task<PagedResult<Correspondent>> GetAllAsync(ISpecification<Correspondent> spec, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all correspondents.
    /// </summary>
    Task<IReadOnlyCollection<Correspondent>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Adds a new correspondent.
    /// </summary>
    Task<Correspondent> AddAsync(Correspondent correspondent, CancellationToken ct = default);

    /// <summary>
    /// Marks an existing correspondent as updated.
    /// </summary>
    void Update(Correspondent correspondent);

    /// <summary>
    /// Soft-deletes a correspondent.
    /// </summary>
    void Delete(Correspondent correspondent);
}
