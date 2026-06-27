namespace Paperless.Core.Documents.Enums;

/// <summary>
/// Identifies how a document was ingested into the system.
/// Maps to the Document.Source choices or the source-of-consumption
/// constants in the original paperless-ngx data model.
/// </summary>
public enum DocumentSource
{
    /// <summary>Document consumed from an email message.</summary>
    ConsumeMail = 1,

    /// <summary>Document uploaded via the web UI (HTTP upload).</summary>
    ConsumeWebUpload = 2,

    /// <summary>Document consumed from the local file system watch folder.</summary>
    ConsumeFileSystem = 3,

    /// <summary>Document consumed via barcode detection.</summary>
    ConsumeBarcode = 4,
}
