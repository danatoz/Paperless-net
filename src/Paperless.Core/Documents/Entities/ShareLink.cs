namespace Paperless.Core.Documents.Entities;

/// <summary>
/// Represents a shareable link for a specific document.
/// Maps to the ShareLink model from the original paperless-ngx data model.
/// </summary>
public class ShareLink : BaseEntity
{
    /// <summary>
    /// Foreign key to the shared <see cref="Document"/>.
    /// </summary>
    public int DocumentId { get; set; }

    /// <summary>
    /// Navigation property to the associated <see cref="Document"/>.
    /// </summary>
    public Document Document { get; set; } = null!;

    /// <summary>
    /// The unique URL slug for the share link.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// The date and time when the share link was created (in UTC).
    /// </summary>
    public DateTime Created { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The optional expiration date and time for the share link (in UTC).
    /// Null means the link never expires.
    /// </summary>
    public DateTime? Expires { get; set; }

    /// <summary>
    /// The file version to share (archive PDF or original file).
    /// </summary>
    public FileVersionType FileVersion { get; set; }

    /// <summary>
    /// Foreign key to the optional <see cref="ShareLinkBundle"/> this link belongs to.
    /// </summary>
    public int? ShareLinkBundleId { get; set; }

    /// <summary>
    /// Navigation property to the parent <see cref="ShareLinkBundle"/>.
    /// </summary>
    public ShareLinkBundle? ShareLinkBundle { get; set; }

    /// <summary>
    /// The file version to include in the share link.
    /// </summary>
    public enum FileVersionType
    {
        /// <summary>The archived (PDF/A) version of the document.</summary>
        Archive = 0,

        /// <summary>The original uploaded file.</summary>
        Origin = 1,
    }
}
