namespace Paperless.Core.Documents.Enums;

/// <summary>
/// Defines the type of event that triggers a workflow execution.
/// Maps to the WorkflowTrigger.Type choices in the original paperless-ngx data model.
/// </summary>
public enum WorkflowTriggerType
{
    /// <summary>Triggered when document consumption starts.</summary>
    Consumption = 1,

    /// <summary>Triggered when a document is added to the system.</summary>
    DocumentAdded = 2,

    /// <summary>Triggered when a document is updated.</summary>
    DocumentUpdated = 3,
}
