namespace Paperless.Core.Documents.Enums;

/// <summary>
/// Represents the lifecycle status of a background task in the system.
/// Maps to the PaperlessTask.Status choices in the original paperless-ngx data model.
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

    /// <summary>The task failed and is scheduled for a retry.</summary>
    Retrying = 4,
}
