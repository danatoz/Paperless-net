using Paperless.Core.Documents.Entities;

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
    public ActionType Type { get; set; }

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

    /// <summary>
    /// The type of action to execute.
    /// </summary>
    public enum ActionType
    {
        /// <summary>Assign a correspondent to the document.</summary>
        SetCorrespondent = 1,

        /// <summary>Assign a tag to the document.</summary>
        SetTag = 2,

        /// <summary>Assign a document type to the document.</summary>
        SetDocumentType = 3,

        /// <summary>Assign a storage path to the document.</summary>
        SetStoragePath = 4,

        /// <summary>Send a webhook notification.</summary>
        Webhook = 5,
    }
}
