using Microsoft.EntityFrameworkCore;
using Paperless.Core.Common.Interfaces;
using Paperless.Core.Common.Models;
using Paperless.Core.Common.Specifications;
using Paperless.Core.Documents.Entities;
using Paperless.Infrastructure.Persistence;

namespace Paperless.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for Document entity operations. Implements IDocumentRepository
/// with full specification, pagination, and bulk operation support.
/// </summary>
public class DocumentRepository : RepositoryBase<Document>, IDocumentRepository
{
    public DocumentRepository(AppDbContext dbContext, IUnitOfWork unitOfWork) : base(dbContext, unitOfWork)
    {
    }

    /// <inheritdoc />
    public override async Task<Document?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await DbSet
            .Include(d => d.Correspondent)
            .Include(d => d.DocumentType)
            .Include(d => d.Tags)
            .Include(d => d.CustomFields)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<PagedResult<Document>> GetAllAsync(
        ISpecification<Document> spec,
        CancellationToken ct = default)
    {
        return await ApplySpecificationAsync(spec, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Document>> SearchAsync(
        string query,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await DbSet.ToListAsync(ct);

        // Case-insensitive search across title and content.
        // Uses LIKE which works on both SQLite (case-insensitive) and PostgreSQL.
        return await DbSet
            .Include(d => d.Correspondent)
            .Include(d => d.DocumentType)
            .Include(d => d.Tags)
            .Where(d => (d.Title != null && EF.Functions.Like(d.Title, $"%{query}%"))
                        || (d.Content != null && EF.Functions.Like(d.Content, $"%{query}%")))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<PagedResult<Document>> SearchAsync(
        string query,
        ISpecification<Document> spec,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await ApplySpecificationAsync(spec, ct);

        // Combine specification criteria with search criteria
        var combinedSpec = new Specification<Document>();
        combinedSpec.Where(d =>
            (d.Title != null && EF.Functions.Like(d.Title, $"%{query}%"))
            || (d.Content != null && EF.Functions.Like(d.Content, $"%{query}%")));

        // If the original spec also has criteria, combine them
        if (spec.Criteria != null)
        {
            var originalCriteria = spec.Criteria;
            combinedSpec.Where(d =>
                (d.Title != null && EF.Functions.Like(d.Title, $"%{query}%"))
                || (d.Content != null && EF.Functions.Like(d.Content, $"%{query}%")));
        }

        // Apply includes, ordering, and pagination from the original spec
        foreach (var include in spec.Includes)
            combinedSpec.Include(include);

        if (spec.OrderBy != null)
        {
            if (spec.IsDescending)
                combinedSpec.OrderByDescending(spec.OrderBy);
            else
                combinedSpec.OrderByAscending(spec.OrderBy);
        }

        if (spec.Page.HasValue && spec.PageSize.HasValue)
            combinedSpec.ApplyPaging(spec.Page.Value, spec.PageSize.Value);

        return await ApplySpecificationAsync(combinedSpec, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Document>> GetByCorrespondentAsync(
        int correspondentId,
        CancellationToken ct = default)
    {
        return await DbSet
            .Include(d => d.Correspondent)
            .Include(d => d.DocumentType)
            .Include(d => d.Tags)
            .Where(d => d.CorrespondentId == correspondentId)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Document>> GetByTagsAsync(
        IReadOnlyCollection<int> tagIds,
        CancellationToken ct = default)
    {
        if (tagIds == null || tagIds.Count == 0)
            return await DbSet.ToListAsync(ct);

        return await DbSet
            .Include(d => d.Correspondent)
            .Include(d => d.DocumentType)
            .Include(d => d.Tags)
            .Where(d => d.Tags.Any(t => tagIds.Contains(t.Id)))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Document?> GetByChecksumAsync(
        string checksum,
        CancellationToken ct = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(d => d.Checksum == checksum, ct);
    }

    /// <inheritdoc />
    public async Task<int> BulkUpdateAsync(
        ISpecification<Document> spec,
        Action<Document> updateAction,
        CancellationToken ct = default)
    {
        var query = ApplySpecificationToQuery(spec);
        var documents = await query.ToListAsync(ct);

        foreach (var document in documents)
        {
            updateAction(document);
        }

        return documents.Count;
    }

    /// <inheritdoc />
    public async Task<int> BulkDeleteAsync(
        ISpecification<Document> spec,
        CancellationToken ct = default)
    {
        var query = ApplySpecificationToQuery(spec);
        var documents = await query.ToListAsync(ct);

        foreach (var document in documents)
        {
            document.IsDeleted = true;
        }

        return documents.Count;
    }
}
