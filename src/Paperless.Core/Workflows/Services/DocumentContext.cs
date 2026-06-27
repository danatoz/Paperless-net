using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Workflows.Services;

/// <summary>
/// The context passed through workflow evaluation and action execution.
/// Carries the document being processed, a record of changes, and optional user info.
/// Maps to the processing context in documents/workflows/.
/// </summary>
public sealed record DocumentContext
{
    /// <summary>
    /// The document being processed by the workflow.
    /// </summary>
    public required Document Document { get; init; }

    /// <summary>
    /// The type of event that triggered this workflow evaluation.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Optional ID of the user who triggered the workflow.
    /// </summary>
    public int? UserId { get; init; }

    /// <summary>
    /// A dictionary of changes that have been applied to the document metadata.
    /// Key: property name, Value: (oldValue, newValue).
    /// </summary>
    public Dictionary<string, (object? OldValue, object? NewValue)> Changes { get; init; } = new();
}
