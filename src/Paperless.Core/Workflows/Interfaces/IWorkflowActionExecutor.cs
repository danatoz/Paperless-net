using Paperless.Core.Workflows.Entities;
using Paperless.Core.Workflows.Services;

namespace Paperless.Core.Workflows.Interfaces;

/// <summary>
/// Executes a <see cref="WorkflowAction"/> against a <see cref="DocumentContext"/>.
/// Maps to the action execution logic from documents/workflows/actions.py and documents/workflows/mutations.py.
/// </summary>
public interface IWorkflowActionExecutor
{
    /// <summary>
    /// Executes the given workflow action against the document context.
    /// </summary>
    /// <param name="action">The workflow action to execute.</param>
    /// <param name="context">The document processing context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the action execution.</returns>
    Task<WorkflowActionResult> ExecuteAsync(WorkflowAction action, DocumentContext context, CancellationToken ct = default);
}
