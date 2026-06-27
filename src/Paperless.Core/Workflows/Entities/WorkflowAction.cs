using Paperless.Core.Documents.Entities;
using Paperless.Core.Documents.Enums;

namespace Paperless.Core.Workflows.Entities;

/// <summary>
/// Defines an action to be executed when a workflow trigger fires.
/// Maps to the WorkflowAction model from the original paperless-ngx data model.
/// </summary>
public class WorkflowAction : BaseEntity
{
    /// <summary>
    /// The type of action to perform.
    /// </summary>
    public WorkflowActionType Type { get; set; }

    /// <summary>
    /// The execution order of this action within the workflow.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Foreign key to the parent <see cref="Workflow"/>.
    /// </summary>
    public int WorkflowId { get; set; }

    /// <summary>
    /// Navigation property to the parent <see cref="Workflow"/>.
    /// </summary>
    public Workflow Workflow { get; set; } = null!;

    /// <summary>
    /// JSON-serialized parameters specific to the action type.
    /// Contains configuration such as target correspondent/tag/document type/storage path IDs,
    /// webhook URL and payload, title template, etc.
    /// </summary>
    public string? ActionParameters { get; set; }
}
