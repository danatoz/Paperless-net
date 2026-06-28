using Paperless.Core.Workflows.Entities;

namespace Paperless.Core.Common.Interfaces;

/// <summary>
/// Repository interface for Workflow entity operations.
/// </summary>
public interface IWorkflowRepository
{
    /// <summary>
    /// Gets a workflow by its unique identifier.
    /// </summary>
    Task<Workflow?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all workflows, eagerly including triggers and actions.
    /// </summary>
    Task<IReadOnlyCollection<Workflow>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves all enabled workflows, ordered by their execution order.
    /// </summary>
    Task<IReadOnlyCollection<Workflow>> GetEnabledWorkflowsAsync(CancellationToken ct = default);

    /// <summary>
    /// Adds a new workflow.
    /// </summary>
    Task<Workflow> AddAsync(Workflow workflow, CancellationToken ct = default);

    /// <summary>
    /// Marks an existing workflow as updated.
    /// </summary>
    void Update(Workflow workflow);

    /// <summary>
    /// Soft-deletes a workflow.
    /// </summary>
    void Delete(Workflow workflow);
}
