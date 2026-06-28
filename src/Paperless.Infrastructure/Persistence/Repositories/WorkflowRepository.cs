using Microsoft.EntityFrameworkCore;
using Paperless.Core.Common.Interfaces;
using Paperless.Core.Workflows.Entities;
using Paperless.Infrastructure.Persistence;

namespace Paperless.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for Workflow entity operations.
/// </summary>
public class WorkflowRepository : RepositoryBase<Workflow>, IWorkflowRepository
{
    public WorkflowRepository(AppDbContext dbContext, IUnitOfWork unitOfWork) : base(dbContext, unitOfWork)
    {
    }

    /// <inheritdoc />
    public override async Task<Workflow?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await DbSet
            .Include(w => w.Triggers)
            .Include(w => w.Actions)
            .FirstOrDefaultAsync(w => w.Id == id, ct);
    }

    /// <inheritdoc />
    public override async Task<IReadOnlyCollection<Workflow>> GetAllAsync(CancellationToken ct = default)
    {
        return await DbSet
            .Include(w => w.Triggers)
            .Include(w => w.Actions)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Workflow>> GetEnabledWorkflowsAsync(CancellationToken ct = default)
    {
        return await DbSet
            .Include(w => w.Triggers)
            .Include(w => w.Actions)
            .Where(w => w.Enabled)
            .OrderBy(w => w.Order)
            .ToListAsync(ct);
    }
}
