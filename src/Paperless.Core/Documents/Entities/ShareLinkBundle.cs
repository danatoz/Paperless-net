namespace Paperless.Core.Documents.Entities;

/// <summary>
/// Represents a bundle of multiple share links, typically for bulk sharing.
/// Maps to the ShareLinkBundle model from the original paperless-ngx data model.
/// </summary>
public class ShareLinkBundle : BaseEntity
{
    /// <summary>
    /// The unique URL slug for the share link bundle.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// The date and time when the bundle was created (in UTC).
    /// </summary>
    public DateTime Created { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The optional expiration date and time for all links in the bundle (in UTC).
    /// Null means the links never expire.
    /// </summary>
    public DateTime? Expires { get; set; }

    // ── Navigation ─────────────────────────────────────────────────

    /// <summary>
    /// The collection of share links belonging to this bundle.
    /// </summary>
    public ICollection<ShareLink> Links { get; set; } = new List<ShareLink>();
}
