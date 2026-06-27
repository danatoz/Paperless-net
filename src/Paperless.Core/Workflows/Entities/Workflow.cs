using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Workflows.Entities;

/// <summary>
/// Represents a workflow that defines automated document processing rules.
/// A workflow consists of triggers (when to run) and actions (what to do).
/// Maps to the Workflow model from the original paperless-ngx data model.
/// </summary>
public class Workflow : BaseEntity
{
    /// <summary>
    /// The display name of the workflow.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The execution order of the workflow relative to other workflows.
    /// Lower numbers execute first.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Whether this workflow is currently enabled and active.
    /// </summary>
    public bool Enabled { get; set; } = true;

    // ── Navigation ─────────────────────────────────────────────────

    /// <summary>
    /// The triggers that determine when this workflow should execute.
    /// </summary>
    public ICollection<WorkflowTrigger> Triggers { get; set; } = new List<WorkflowTrigger>();

    /// <summary>
    /// The actions that this workflow performs when triggered.
    /// </summary>
    public ICollection<WorkflowAction> Actions { get; set; } = new List<WorkflowAction>();
}
