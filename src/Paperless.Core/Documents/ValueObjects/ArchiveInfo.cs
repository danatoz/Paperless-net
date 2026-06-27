using Paperless.Shared.Abstractions;

namespace Paperless.Core.Documents.ValueObjects;

/// <summary>
/// Value object containing information about a document's archived (PDF/A) version.
/// Maps to the archive metadata fields from the original paperless-ngx data model.
/// </summary>
public sealed class ArchiveInfo : ValueObject
{
    /// <summary>
    /// Gets the SHA-256 checksum of the archived PDF/A file.
    /// </summary>
    public Checksum? ArchiveChecksum { get; }

    /// <summary>
    /// Gets the filesystem path (or storage key) where the archive file is stored.
    /// </summary>
    public string? ArchivePath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveInfo"/> class.
    /// </summary>
    /// <param name="archiveChecksum">The SHA-256 checksum of the archive file.</param>
    /// <param name="archivePath">The path or storage key of the archive file.</param>
    public ArchiveInfo(Checksum? archiveChecksum, string? archivePath)
    {
        ArchiveChecksum = archiveChecksum;
        ArchivePath = archivePath;
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ArchiveChecksum;
        yield return ArchivePath;
    }
}
