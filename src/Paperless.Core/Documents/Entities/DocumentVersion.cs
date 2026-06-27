namespace Paperless.Core.Documents.Entities;

/// <summary>
/// Represents a specific version of a document's file.
/// Maps to the DocumentVersion model from the original paperless-ngx data model.
/// </summary>
public class DocumentVersion
{
    /// <summary>
    /// Unique identifier for this version record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the parent <see cref="Document"/>.
    /// </summary>
    public int DocumentId { get; set; }

    /// <summary>
    /// Navigation property to the parent <see cref="Document"/>.
    /// </summary>
    public Document Document { get; set; } = null!;

    /// <summary>
    /// Sequential version number (1, 2, 3, ...) within the document's version history.
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// The filename of this version.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// The file size in bytes.
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Timestamp when this version was created (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
