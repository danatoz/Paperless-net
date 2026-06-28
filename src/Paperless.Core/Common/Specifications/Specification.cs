using System.Linq.Expressions;

namespace Paperless.Core.Common.Specifications;

/// <summary>
/// Base class for specifications. Provides a fluent-like constructor
/// for building common query patterns.
/// </summary>
/// <typeparam name="T">The entity type the specification applies to.</typeparam>
public class Specification<T> : ISpecification<T>
{
    /// <inheritdoc />
    public Expression<Func<T, bool>>? Criteria { get; private set; }

    /// <inheritdoc />
    public IReadOnlyCollection<Expression<Func<T, object?>>> Includes => _includes.AsReadOnly();
    private readonly List<Expression<Func<T, object?>>> _includes = new();

    /// <inheritdoc />
    public Expression<Func<T, object?>>? OrderBy { get; private set; }

    /// <inheritdoc />
    public bool IsDescending { get; private set; }

    /// <inheritdoc />
    public int? Page { get; private set; }

    /// <inheritdoc />
    public int? PageSize { get; private set; }

    /// <summary>
    /// Creates a new empty specification.
    /// </summary>
    public Specification()
    {
    }

    /// <summary>
    /// Creates a specification with the given filter criteria.
    /// </summary>
    public Specification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    /// <summary>
    /// Sets the filter criteria.
    /// </summary>
    public Specification<T> Where(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
        return this;
    }

    /// <summary>
    /// Adds an include expression for eager-loading.
    /// </summary>
    public Specification<T> Include(Expression<Func<T, object?>> includeExpression)
    {
        _includes.Add(includeExpression);
        return this;
    }

    /// <summary>
    /// Sets the ordering expression.
    /// </summary>
    public Specification<T> OrderByAscending(Expression<Func<T, object?>> orderByExpression)
    {
        OrderBy = orderByExpression;
        IsDescending = false;
        return this;
    }

    /// <summary>
    /// Sets the ordering expression with descending direction.
    /// </summary>
    public Specification<T> OrderByDescending(Expression<Func<T, object?>> orderByExpression)
    {
        OrderBy = orderByExpression;
        IsDescending = true;
        return this;
    }

    /// <summary>
    /// Sets pagination parameters.
    /// </summary>
    public Specification<T> ApplyPaging(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
        return this;
    }
}
