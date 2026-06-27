namespace Paperless.Core.Documents.Enums;

/// <summary>
/// Defines the type of action that a workflow can execute.
/// Maps to the WorkflowAction.Type choices in the original paperless-ngx data model.
/// </summary>
public enum WorkflowActionType
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
