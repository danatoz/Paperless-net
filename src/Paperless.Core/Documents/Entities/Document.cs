namespace Paperless.Core.Documents.Entities;

/// <summary>
/// Central domain entity representing a document in the system.
/// Maps to the Document model from the original paperless-ngx data model.
/// </summary>
public class Document : BaseEntity
{
    /// <summary>
    /// The display title of the document.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The full OCR-extracted text content of the document.
    /// </summary>
    public string? Content { get; set; }

    // ── Relationships ──────────────────────────────────────────────

    /// <summary>
    /// Foreign key to the <see cref="Correspondent"/>.
    /// </summary>
    public int? CorrespondentId { get; set; }

    /// <summary>
    /// Navigation property to the associated <see cref="Correspondent"/>.
    /// </summary>
    public Correspondent? Correspondent { get; set; }

    /// <summary>
    /// Foreign key to the <see cref="DocumentType"/>.
    /// </summary>
    public int? DocumentTypeId { get; set; }

    /// <summary>
    /// Navigation property to the associated <see cref="DocumentType"/>.
    /// </summary>
    public DocumentType? DocumentType { get; set; }

    /// <summary>
    /// Many-to-many: the tags assigned to this document.
    /// </summary>
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();

    /// <summary>
    /// Many-to-many via join entity: the custom field values for this document.
    /// </summary>
    public ICollection<DocumentCustomField> CustomFields { get; set; } = new List<DocumentCustomField>();

    /// <summary>
    /// One-to-many: the version history for this document.
    /// </summary>
    public ICollection<DocumentVersion> DocumentVersions { get; set; } = new List<DocumentVersion>();

    // ── Timestamps ─────────────────────────────────────────────────

    /// <summary>
    /// The date shown on the document (may differ from the date it was added).
    /// </summary>
    public DateTime? Created { get; set; }

    /// <summary>
    /// The date the document was added to the system.
    /// </summary>
    public DateTime Added { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The date the document was last modified.
    /// </summary>
    public DateTime Modified { get; set; } = DateTime.UtcNow;

    // ── File metadata ──────────────────────────────────────────────

    /// <summary>
    /// SHA-256 checksum of the original uploaded file.
    /// Used for deduplication and integrity verification.
    /// </summary>
    public string? Checksum { get; set; }

    /// <summary>
    /// SHA-256 checksum of the archived (PDF/A) version.
    /// </summary>
    public string? ArchiveChecksum { get; set; }

    /// <summary>
    /// The original filename as uploaded.
    /// </summary>
    public string? Filename { get; set; }

    /// <summary>
    /// The storage path pattern result — the filesystem path where the document files are stored.
    /// </summary>
    public string? StoragePath { get; set; }

    // ── Ownership & deletion ───────────────────────────────────────

    /// <summary>
    /// Foreign key to the owner user.
    /// </summary>
    public int? OwnerId { get; set; }

    // ── Archive Serial Number ───────────────────────────────────────

    /// <summary>
    /// Archive Serial Number — a user-assigned unique number for physical filing.
    /// </summary>
    public int? ArchiveSerialNumber { get; set; }
}
