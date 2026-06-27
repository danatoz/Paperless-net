namespace Paperless.Core.Documents.Enums;

/// <summary>
/// Determines which version of a document is shared via a share link.
/// Maps to the ShareLink.FileVersion choices in the original paperless-ngx data model.
/// </summary>
public enum ShareLinkFileVersion
{
    /// <summary>The archived (PDF/A) version of the document.</summary>
    Archive = 0,

    /// <summary>The original uploaded file.</summary>
    Origin = 1,
}
