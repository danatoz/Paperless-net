using Microsoft.EntityFrameworkCore;
using Paperless.Core.Common.Interfaces;
using Paperless.Core.Documents.Entities;
using Paperless.Infrastructure.Persistence;

namespace Paperless.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for SavedView entity operations.
/// </summary>
public class SavedViewRepository : RepositoryBase<SavedView>, ISavedViewRepository
{
    public SavedViewRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<SavedView>> GetDashboardViewsAsync(CancellationToken ct = default)
    {
        return await DbSet
            .Where(sv => sv.ShowInDashboard)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<SavedView>> GetSidebarViewsAsync(CancellationToken ct = default)
    {
        return await DbSet
            .Where(sv => sv.ShowInSidebar)
            .ToListAsync(ct);
    }
}
