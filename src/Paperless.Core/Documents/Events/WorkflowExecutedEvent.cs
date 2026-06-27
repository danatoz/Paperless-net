using MediatR;

namespace Paperless.Core.Documents.Events;

/// <summary>
/// Domain event raised after a workflow has been executed for a document.
/// </summary>
/// <param name="WorkflowId">The unique identifier of the workflow.</param>
/// <param name="WorkflowName">The display name of the workflow.</param>
/// <param name="DocumentId">The identifier of the document processed.</param>
/// <param name="SuccessCount">The number of actions that succeeded.</param>
/// <param name="FailureCount">The number of actions that failed.</param>
public sealed record WorkflowExecutedEvent(
    int WorkflowId,
    string WorkflowName,
    int DocumentId,
    int SuccessCount,
    int FailureCount) : INotification;
