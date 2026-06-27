namespace Paperless.Core.Documents.Entities;

/// <summary>
/// Tracks the status and progress of background tasks in the system.
/// Maps to the PaperlessTask model from the original paperless-ngx data model.
/// </summary>
public class PaperlessTask : BaseEntity
{
    /// <summary>
    /// The external task identifier (e.g., Hangfire job ID).
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable name for the task.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The current status of the task.
    /// </summary>
    public TaskStatus Status { get; set; }

    /// <summary>
    /// Whether the task result has been acknowledged by the user.
    /// </summary>
    public bool Acknowledged { get; set; }

    /// <summary>
    /// The date and time when the task completed (in UTC).
    /// </summary>
    public DateTime? Done { get; set; }

    /// <summary>
    /// JSON-serialized result data from the task execution.
    /// Contains output such as document IDs, error messages, etc.
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Enumeration of possible task statuses.
    /// </summary>
    public enum TaskStatus
    {
        /// <summary>The task is queued and waiting to start.</summary>
        Pending = 0,

        /// <summary>The task has started execution.</summary>
        Started = 1,

        /// <summary>The task completed successfully.</summary>
        Complete = 2,

        /// <summary>The task failed during execution.</summary>
        Failed = 3,
    }
}
