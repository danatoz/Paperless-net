using Microsoft.EntityFrameworkCore;
using Paperless.Core.Common.Interfaces;
using Paperless.Core.Documents.Entities;
using Paperless.Infrastructure.Persistence;

namespace Paperless.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for ShareLink and ShareLinkBundle entity operations.
/// </summary>
public class ShareLinkRepository : RepositoryBase<ShareLink>, IShareLinkRepository
{
    public ShareLinkRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    /// <inheritdoc />
    public override async Task<ShareLink?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await DbSet
            .Include(sl => sl.Document)
            .Include(sl => sl.ShareLinkBundle)
            .FirstOrDefaultAsync(sl => sl.Id == id, ct);
    }

    /// <inheritdoc />
    public override async Task<IReadOnlyCollection<ShareLink>> GetAllAsync(CancellationToken ct = default)
    {
        return await DbSet
            .Include(sl => sl.Document)
            .Include(sl => sl.ShareLinkBundle)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<ShareLink?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await DbSet
            .Include(sl => sl.Document)
            .Include(sl => sl.ShareLinkBundle)
            .FirstOrDefaultAsync(sl => sl.Slug == slug, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<ShareLink>> GetByDocumentIdAsync(
        int documentId,
        CancellationToken ct = default)
    {
        return await DbSet
            .Include(sl => sl.Document)
            .Include(sl => sl.ShareLinkBundle)
            .Where(sl => sl.DocumentId == documentId)
            .ToListAsync(ct);
    }

    // ── Bundle operations ───────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ShareLinkBundle?> GetBundleByIdAsync(int id, CancellationToken ct = default)
    {
        return await DbContext.ShareLinkBundles
            .Include(b => b.Links)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<ShareLinkBundle?> GetBundleBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await DbContext.ShareLinkBundles
            .Include(b => b.Links)
            .FirstOrDefaultAsync(b => b.Slug == slug, ct);
    }

    /// <inheritdoc />
    public async Task<ShareLinkBundle> AddBundleAsync(ShareLinkBundle bundle, CancellationToken ct = default)
    {
        await DbContext.ShareLinkBundles.AddAsync(bundle, ct);
        return bundle;
    }

    /// <inheritdoc />
    public void DeleteBundle(ShareLinkBundle bundle)
    {
        bundle.IsDeleted = true;
        DbContext.ShareLinkBundles.Update(bundle);
    }
}
