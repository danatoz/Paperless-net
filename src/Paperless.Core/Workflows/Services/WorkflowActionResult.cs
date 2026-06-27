using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Workflows.Services;

/// <summary>
/// Represents the result of executing a single workflow action.
/// Maps to the action result from documents/workflows/actions.py.
/// </summary>
public sealed record WorkflowActionResult
{
    /// <summary>
    /// Whether the action executed successfully.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// A human-readable message describing the result.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// If an error occurred, the error details.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Modifications to the document that were applied by this action.
    /// </summary>
    public DocumentModifications? Modifications { get; init; }

    /// <summary>
    /// Creates a successful result with optional modifications.
    /// </summary>
    public static WorkflowActionResult Success(string message, DocumentModifications? modifications = null) =>
        new() { IsSuccess = true, Message = message, Modifications = modifications };

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static WorkflowActionResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Describes modifications to be applied to a document as a result of a workflow action.
/// </summary>
public sealed record DocumentModifications
{
    /// <summary>
    /// The ID of the correspondent to assign, if any.
    /// </summary>
    public int? CorrespondentId { get; init; }

    /// <summary>
    /// The ID of the document type to assign, if any.
    /// </summary>
    public int? DocumentTypeId { get; init; }

    /// <summary>
    /// The IDs of the tags to assign, if any.
    /// </summary>
    public IReadOnlyCollection<int>? TagIds { get; init; }

    /// <summary>
    /// The ID of the storage path to assign, if any.
    /// </summary>
    public int? StoragePathId { get; init; }

    /// <summary>
    /// The webhook URL to call, if this action sends a webhook.
    /// </summary>
    public string? WebhookUrl { get; init; }

    /// <summary>
    /// The JSON payload to send with the webhook, if applicable.
    /// </summary>
    public string? WebhookPayload { get; init; }
}
