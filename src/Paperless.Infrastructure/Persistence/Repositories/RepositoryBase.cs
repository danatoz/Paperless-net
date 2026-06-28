using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Paperless.Core.Common.Models;
using Paperless.Core.Common.Specifications;
using Paperless.Core.Documents.Entities;
using Paperless.Infrastructure.Persistence;

namespace Paperless.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository class providing common CRUD and query operations
/// using the Specification pattern.
/// </summary>
/// <typeparam name="T">The entity type, must inherit from <see cref="BaseEntity"/>.</typeparam>
public abstract class RepositoryBase<T>
    where T : BaseEntity
{
    protected readonly AppDbContext DbContext;
    protected readonly DbSet<T> DbSet;

    protected RepositoryBase(AppDbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<T>();
    }

    /// <summary>
    /// Applies the specification's includes, filter, ordering, and pagination
    /// to the base query and returns the result as a <see cref="PagedResult{T}"/>.
    /// </summary>
    protected async Task<PagedResult<T>> ApplySpecificationAsync(
        ISpecification<T> spec,
        CancellationToken ct = default)
    {
        var query = ApplySpecificationToQuery(spec);

        var totalCount = await query.CountAsync(ct);

        // Apply pagination
        if (spec.Page.HasValue && spec.PageSize.HasValue)
        {
            query = query
                .Skip((spec.Page.Value - 1) * spec.PageSize.Value)
                .Take(spec.PageSize.Value);
        }

        var items = await query.ToListAsync(ct);

        return new PagedResult<T>
        {
            Items = items.AsReadOnly(),
            Page = spec.Page ?? 1,
            PageSize = spec.PageSize ?? totalCount,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Applies the specification's includes, filter, and ordering to the query
    /// and returns the full list (no pagination).
    /// </summary>
    protected async Task<IReadOnlyCollection<T>> ApplySpecificationToListAsync(
        ISpecification<T> spec,
        CancellationToken ct = default)
    {
        var query = ApplySpecificationToQuery(spec);
        return await query.ToListAsync(ct);
    }

    /// <summary>
    /// Builds an IQueryable from the given specification (includes, filter, order).
    /// </summary>
    protected IQueryable<T> ApplySpecificationToQuery(ISpecification<T> spec)
    {
        IQueryable<T> query = DbSet.AsQueryable();

        // Apply includes
        foreach (var include in spec.Includes)
        {
            query = query.Include(include);
        }

        // Apply filter
        if (spec.Criteria != null)
        {
            query = query.Where(spec.Criteria);
        }

        // Apply ordering
        if (spec.OrderBy != null)
        {
            query = spec.IsDescending
                ? query.OrderByDescending(spec.OrderBy)
                : query.OrderBy(spec.OrderBy);
        }

        return query;
    }

    /// <summary>
    /// Gets an entity by its primary key.
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await DbSet.FindAsync(new object[] { id }, ct);
    }

    /// <summary>
    /// Gets all entities with optional includes.
    /// </summary>
    public virtual async Task<IReadOnlyCollection<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await DbSet.ToListAsync(ct);
    }

    /// <summary>
    /// Adds a new entity to the context.
    /// </summary>
    public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await DbSet.AddAsync(entity, ct);
        return entity;
    }

    /// <summary>
    /// Marks an entity as updated in the change tracker.
    /// </summary>
    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    /// <summary>
    /// Soft-deletes an entity by setting IsDeleted flag.
    /// </summary>
    public virtual void Delete(T entity)
    {
        entity.IsDeleted = true;
        DbSet.Update(entity);
    }
}
