using MediatR;
using Paperless.Core.Documents.Events;
using Paperless.Core.Workflows.Entities;
using Paperless.Core.Workflows.Interfaces;

namespace Paperless.Core.Workflows.Services;

/// <summary>
/// Orchestrates workflow processing for a document.
/// For each enabled workflow (ordered by <see cref="Workflow.Order"/>),
/// evaluates triggers and executes matching actions.
/// Maps to the workflow engine logic from documents/workflows/actions.py.
/// </summary>
public sealed class WorkflowEngine
{
    private readonly IWorkflowTriggerEvaluator _triggerEvaluator;
    private readonly IWorkflowActionExecutor _actionExecutor;
    private readonly IPublisher _publisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowEngine"/> class.
    /// </summary>
    /// <param name="triggerEvaluator">The trigger evaluator.</param>
    /// <param name="actionExecutor">The action executor.</param>
    /// <param name="publisher">MediatR publisher for domain events.</param>
    public WorkflowEngine(
        IWorkflowTriggerEvaluator triggerEvaluator,
        IWorkflowActionExecutor actionExecutor,
        IPublisher publisher)
    {
        _triggerEvaluator = triggerEvaluator ?? throw new ArgumentNullException(nameof(triggerEvaluator));
        _actionExecutor = actionExecutor ?? throw new ArgumentNullException(nameof(actionExecutor));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    /// <summary>
    /// Processes a document through all applicable workflows.
    /// </summary>
    /// <param name="context">The document context to process.</param>
    /// <param name="workflows">The list of all available workflows.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of action results from all triggered workflows.
    /// A failure in one workflow does not block other workflows.</returns>
    public async Task<IReadOnlyList<WorkflowActionResult>> ProcessDocumentAsync(
        DocumentContext context,
        IEnumerable<Workflow> workflows,
        CancellationToken ct = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (workflows is null) throw new ArgumentNullException(nameof(workflows));

        var results = new List<WorkflowActionResult>();

        // Process workflows in order; skip disabled ones
        var enabledWorkflows = workflows
            .Where(w => w.Enabled)
            .OrderBy(w => w.Order)
            .ToList();

        foreach (var workflow in enabledWorkflows)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // Check if any trigger matches
                var triggerMatched = await EvaluateTriggersAsync(workflow.Triggers, context, ct);

                if (!triggerMatched)
                    continue;

                // Execute all actions for this workflow
                var orderedActions = workflow.Actions
                    .OrderBy(a => a.Order)
                    .ToList();

                foreach (var action in orderedActions)
                {
                    ct.ThrowIfCancellationRequested();

                    var actionResult = await _actionExecutor.ExecuteAsync(action, context, ct);
                    results.Add(actionResult);

                    // Apply modifications to the document context for subsequent actions
                    if (actionResult.IsSuccess && actionResult.Modifications is not null)
                    {
                        ApplyModifications(context, actionResult.Modifications);
                    }
                }

                // Publish domain event for this workflow execution
                var workflowEvent = new WorkflowExecutedEvent(
                    workflow.Id,
                    workflow.Name,
                    context.Document.Id,
                    results.Count(r => r.IsSuccess),
                    results.Count(r => !r.IsSuccess));

                await _publisher.Publish(workflowEvent, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Failure in one workflow does not block the rest
                results.Add(WorkflowActionResult.Failure(
                    $"Workflow '{workflow.Name}' failed: {ex.Message}"));
            }
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Evaluates all triggers for a workflow. Returns true if any trigger matches.
    /// </summary>
    private async Task<bool> EvaluateTriggersAsync(
        ICollection<WorkflowTrigger> triggers,
        DocumentContext context,
        CancellationToken ct)
    {
        foreach (var trigger in triggers)
        {
            var matched = await _triggerEvaluator.EvaluateAsync(trigger, context, ct);
            if (matched)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Applies document modifications from an action result to the context.
    /// This allows subsequent actions in the same workflow to see the updated state.
    /// </summary>
    private static void ApplyModifications(DocumentContext context, DocumentModifications modifications)
    {
        if (modifications.CorrespondentId.HasValue)
        {
            var oldValue = context.Document.CorrespondentId;
            context.Document.CorrespondentId = modifications.CorrespondentId.Value;
            context.Changes[$"{nameof(context.Document.CorrespondentId)}"] = (oldValue, modifications.CorrespondentId.Value);
        }

        if (modifications.DocumentTypeId.HasValue)
        {
            var oldValue = context.Document.DocumentTypeId;
            context.Document.DocumentTypeId = modifications.DocumentTypeId.Value;
            context.Changes[$"{nameof(context.Document.DocumentTypeId)}"] = (oldValue, modifications.DocumentTypeId.Value);
        }
    }
}
