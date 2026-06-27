using Paperless.Core.Documents.Entities;
using Paperless.Core.Documents.Enums;

namespace Paperless.Core.Workflows.Entities;

/// <summary>
/// Defines a condition that triggers a workflow execution.
/// Maps to the WorkflowTrigger model from the original paperless-ngx data model.
/// </summary>
public class WorkflowTrigger : BaseEntity
{
    /// <summary>
    /// The type of event that triggers this workflow.
    /// </summary>
    public WorkflowTriggerType Type { get; set; }

    /// <summary>
    /// Foreign key to the parent <see cref="Workflow"/>.
    /// </summary>
    public int WorkflowId { get; set; }

    /// <summary>
    /// Navigation property to the parent <see cref="Workflow"/>.
    /// </summary>
    public Workflow Workflow { get; set; } = null!;

    // ── Matching fields ─────────────────────────────────────────────

    /// <summary>
    /// The match pattern for filtering which documents this trigger applies to.
    /// </summary>
    public string? Match { get; set; }

    /// <summary>
    /// The matching algorithm used to evaluate the <see cref="Match"/> pattern.
    /// Uses the same values as <see cref="Documents.Enums.MatchingAlgorithm"/>.
    /// </summary>
    public int MatchingAlgorithm { get; set; }

    /// <summary>
    /// Whether matching should be case-insensitive.
    /// </summary>
    public bool IsInsensitive { get; set; } = true;

    // ── Filter rules (JSON) ─────────────────────────────────────────

    /// <summary>
    /// JSON-serialized filter rules for this trigger. Contains conditions
    /// such as correspondent, tags, document type, storage path, etc.
    /// </summary>
    public string? FilterRules { get; set; }

    /// <summary>
    /// Filter by source document path (supports wildcards).
    /// </summary>
    public string? FilterPath { get; set; }

    /// <summary>
    /// Filter by source filename (supports wildcards such as *.pdf).
    /// </summary>
    public string? FilterFilename { get; set; }

    /// <summary>
    /// JSON-serialized document source filters (e.g., consume folder, API upload, mail fetch, web UI).
    /// </summary>
    public string? Sources { get; set; }

    // ── Schedule fields ─────────────────────────────────────────────

    /// <summary>
    /// Number of days offset from the trigger date for scheduled triggers.
    /// </summary>
    public int? ScheduleOffsetDays { get; set; }

    /// <summary>
    /// Whether the scheduled trigger should repeat.
    /// </summary>
    public bool ScheduleIsRecurring { get; set; }

    /// <summary>
    /// The interval in days between recurring scheduled triggers.
    /// </summary>
    public int? ScheduleRecurringIntervalDays { get; set; }
}
