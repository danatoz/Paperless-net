using System.Linq.Expressions;

namespace Paperless.Core.Common.Specifications;

/// <summary>
/// Defines a specification pattern interface for building composable queries.
/// Allows filtering, sorting, and eager-loading of related entities.
/// </summary>
/// <typeparam name="T">The entity type the specification applies to.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// The filter criteria expression (WHERE clause).
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// A list of include expressions for eager-loading related entities (Include/ThenInclude).
    /// </summary>
    IReadOnlyCollection<Expression<Func<T, object?>>> Includes { get; }

    /// <summary>
    /// The ordering expression and direction (ORDER BY).
    /// </summary>
    Expression<Func<T, object?>>? OrderBy { get; }

    /// <summary>
    /// Whether the ordering is descending.
    /// </summary>
    bool IsDescending { get; }

    /// <summary>
    /// The page number for pagination (1-based). Null means no pagination.
    /// </summary>
    int? Page { get; }

    /// <summary>
    /// The page size for pagination. Null means no pagination.
    /// </summary>
    int? PageSize { get; }
}
