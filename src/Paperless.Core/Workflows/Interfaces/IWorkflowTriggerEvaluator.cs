using Paperless.Core.Workflows.Entities;
using Paperless.Core.Workflows.Services;

namespace Paperless.Core.Workflows.Interfaces;

/// <summary>
/// Evaluates whether a <see cref="WorkflowTrigger"/> matches a given <see cref="DocumentContext"/>.
/// Maps to the trigger evaluation logic from documents/workflows/actions.py.
/// </summary>
public interface IWorkflowTriggerEvaluator
{
    /// <summary>
    /// Determines whether the given trigger matches the document context.
    /// </summary>
    /// <param name="trigger">The workflow trigger to evaluate.</param>
    /// <param name="context">The document processing context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the trigger conditions are satisfied; otherwise false.</returns>
    Task<bool> EvaluateAsync(WorkflowTrigger trigger, DocumentContext context, CancellationToken ct = default);
}
