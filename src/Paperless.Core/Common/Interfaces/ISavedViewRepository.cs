using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Common.Interfaces;

/// <summary>
/// Repository interface for SavedView entity operations.
/// </summary>
public interface ISavedViewRepository
{
    /// <summary>
    /// Gets a saved view by its unique identifier.
    /// </summary>
    Task<SavedView?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all saved views.
    /// </summary>
    Task<IReadOnlyCollection<SavedView>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets saved views that should appear on the dashboard.
    /// </summary>
    Task<IReadOnlyCollection<SavedView>> GetDashboardViewsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets saved views that should appear in the sidebar.
    /// </summary>
    Task<IReadOnlyCollection<SavedView>> GetSidebarViewsAsync(CancellationToken ct = default);

    /// <summary>
    /// Adds a new saved view.
    /// </summary>
    Task<SavedView> AddAsync(SavedView savedView, CancellationToken ct = default);

    /// <summary>
    /// Marks an existing saved view as updated.
    /// </summary>
    void Update(SavedView savedView);

    /// <summary>
    /// Soft-deletes a saved view.
    /// </summary>
    void Delete(SavedView savedView);
}
