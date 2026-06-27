using MediatR;

namespace Paperless.Shared.Contracts;

/// <summary>
/// Event raised when a background task's status changes.
/// Used to propagate status updates to SignalR and other consumers.
/// </summary>
/// <param name="TaskId">The unique identifier of the background task.</param>
/// <param name="Status">The current status of the task (e.g., "started", "completed", "failed").</param>
/// <param name="Message">A human-readable status message.</param>
/// <param name="Progress">Optional progress percentage (0-100).</param>
public sealed record TaskStatusUpdate(
    Guid TaskId,
    string Status,
    string Message,
    int? Progress = null) : INotification;
