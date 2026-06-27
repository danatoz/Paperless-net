using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Common.Interfaces;

/// <summary>
/// Repository interface for CustomField entity operations.
/// </summary>
public interface ICustomFieldRepository
{
    /// <summary>
    /// Gets a custom field by its unique identifier.
    /// </summary>
    Task<CustomField?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all custom fields.
    /// </summary>
    Task<IReadOnlyCollection<CustomField>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Adds a new custom field.
    /// </summary>
    Task<CustomField> AddAsync(CustomField customField, CancellationToken ct = default);

    /// <summary>
    /// Marks an existing custom field as updated.
    /// </summary>
    void Update(CustomField customField);

    /// <summary>
    /// Soft-deletes a custom field.
    /// </summary>
    void Delete(CustomField customField);
}
