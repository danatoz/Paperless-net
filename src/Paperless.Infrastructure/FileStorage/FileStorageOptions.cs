namespace Paperless.Infrastructure.FileStorage;

/// <summary>
/// Configuration options for file storage paths.
/// Maps to the PAPERLESS_ORIGINS_DIR, PAPERLESS_ARCHIVE_DIR,
/// and thumbnail/preview directories from the original paperless-ngx settings.
/// </summary>
public class FileStorageOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "FileStorage";

    /// <summary>
    /// The base directory on disk where all document files are stored.
    /// Defaults to "./data" which resolves relative to the application's
    /// content root. Equivalent to PAPERLESS_DATA_DIR in the original.
    /// </summary>
    public string BaseDirectory { get; set; } = "./data";

    /// <summary>
    /// Subdirectory within <see cref="BaseDirectory"/> for original uploaded files.
    /// Equivalent to PAPERLESS_ORIGINALS_DIR.
    /// </summary>
    public string OriginalsSubdirectory { get; set; } = "originals";

    /// <summary>
    /// Subdirectory within <see cref="BaseDirectory"/> for archived PDF/A files.
    /// Equivalent to PAPERLESS_ARCHIVE_DIR.
    /// </summary>
    public string ArchiveSubdirectory { get; set; } = "archive";

    /// <summary>
    /// Subdirectory within <see cref="BaseDirectory"/> for document thumbnails.
    /// Equivalent to PAPERLESS_THUMBNAIL_DIR.
    /// </summary>
    public string ThumbnailsSubdirectory { get; set; } = "thumbnails";

    /// <summary>
    /// Subdirectory within <see cref="BaseDirectory"/> for document previews.
    /// </summary>
    public string PreviewsSubdirectory { get; set; } = "previews";

    /// <summary>
    /// Gets the resolved full path for originals storage.
    /// </summary>
    public string OriginalsDirectory => Path.GetFullPath(Path.Combine(BaseDirectory, OriginalsSubdirectory));

    /// <summary>
    /// Gets the resolved full path for archive storage.
    /// </summary>
    public string ArchiveDirectory => Path.GetFullPath(Path.Combine(BaseDirectory, ArchiveSubdirectory));

    /// <summary>
    /// Gets the resolved full path for thumbnails storage.
    /// </summary>
    public string ThumbnailsDirectory => Path.GetFullPath(Path.Combine(BaseDirectory, ThumbnailsSubdirectory));

    /// <summary>
    /// Gets the resolved full path for previews storage.
    /// </summary>
    public string PreviewsDirectory => Path.GetFullPath(Path.Combine(BaseDirectory, PreviewsSubdirectory));
}
