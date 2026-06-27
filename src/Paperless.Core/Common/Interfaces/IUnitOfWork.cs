namespace Paperless.Core.Common.Interfaces;

/// <summary>
/// Unit of Work interface for managing database transactions.
/// Analogous to Django's transaction.atomic() but with explicit async pattern.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    Task CommitAsync(CancellationToken ct = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    Task RollbackAsync(CancellationToken ct = default);
}
