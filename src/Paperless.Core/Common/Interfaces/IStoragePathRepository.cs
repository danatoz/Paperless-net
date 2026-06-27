using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Common.Interfaces;

/// <summary>
/// Repository interface for StoragePath entity operations.
/// </summary>
public interface IStoragePathRepository
{
    /// <summary>
    /// Gets a storage path by its unique identifier.
    /// </summary>
    Task<StoragePath?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all storage paths.
    /// </summary>
    Task<IReadOnlyCollection<StoragePath>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Adds a new storage path.
    /// </summary>
    Task<StoragePath> AddAsync(StoragePath storagePath, CancellationToken ct = default);

    /// <summary>
    /// Marks an existing storage path as updated.
    /// </summary>
    void Update(StoragePath storagePath);

    /// <summary>
    /// Soft-deletes a storage path.
    /// </summary>
    void Delete(StoragePath storagePath);
}
