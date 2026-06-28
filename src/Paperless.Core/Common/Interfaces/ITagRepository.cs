using Paperless.Core.Common.Models;
using Paperless.Core.Common.Specifications;
using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Common.Interfaces;

/// <summary>
/// Repository interface for Tag entity operations.
/// </summary>
public interface ITagRepository
{
    /// <summary>
    /// Gets a tag by its unique identifier.
    /// </summary>
    Task<Tag?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a paginated, filtered, and sorted list of tags
    /// using a specification.
    /// </summary>
    Task<PagedResult<Tag>> GetAllAsync(ISpecification<Tag> spec, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all tags.
    /// </summary>
    Task<IReadOnlyCollection<Tag>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Adds a new tag.
    /// </summary>
    Task<Tag> AddAsync(Tag tag, CancellationToken ct = default);

    /// <summary>
    /// Marks an existing tag as updated.
    /// </summary>
    void Update(Tag tag);

    /// <summary>
    /// Soft-deletes a tag.
    /// </summary>
    void Delete(Tag tag);
}
