using Paperless.Core.Documents.Entities;
using Paperless.Infrastructure.FileStorage;

namespace Paperless.Infrastructure.FileStorage;

/// <summary>
/// Helper class for computing document file paths on storage.
/// Mirrors the path resolution logic from the original paperless-ngx
/// file_handling.py, adapted to the IFileStorage abstraction.
///
/// All returned paths are relative to the FileStorage root and include
/// the type subdirectory (e.g., "originals/0000001.pdf").
/// </summary>
public static class PathResolutionHelper
{
    /// <summary>
    /// Gets the relative storage path for the original (uploaded) file of a document.
    /// </summary>
    /// <param name="document">The document entity.</param>
    /// <returns>
    /// A relative path like "originals/0000001.pdf" or "originals/2024/taxes/report.pdf".
    /// Uses the document's <see cref="Document.StoragePath"/> if set (rendered template),
    /// otherwise falls back to <see cref="Document.Filename"/> or a default based on the document ID.
    /// </returns>
    public static string GetOriginalFilePath(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var options = new FileStorageOptions();
        var subdir = options.OriginalsSubdirectory;

        var relativePath = document.StoragePath
                           ?? document.Filename
                           ?? $"{document.Id:D7}{GetFileExtension(document)}";

        return $"{subdir}/{relativePath}";
    }

    /// <summary>
    /// Gets the relative storage path for the archived (PDF/A) version of a document.
    /// </summary>
    /// <param name="document">The document entity.</param>
    /// <returns>
    /// A relative path like "archive/0000001.pdf".
    /// Falls back to a default path based on the original filename stem + ".pdf".
    /// </returns>
    public static string GetArchiveFilePath(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var options = new FileStorageOptions();
        var subdir = options.ArchiveSubdirectory;

        // Try to derive from the original filename if available
        if (document.Filename is not null)
        {
            var stem = Path.GetFileNameWithoutExtension(document.Filename);
            return $"{subdir}/{stem}.pdf";
        }

        return $"{subdir}/{document.Id:D7}.pdf";
    }

    /// <summary>
    /// Gets the relative storage path for the document thumbnail.
    /// </summary>
    /// <param name="document">The document entity.</param>
    /// <returns>
    /// A relative path like "thumbnails/0000001.png".
    /// </returns>
    public static string GetThumbnailPath(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var options = new FileStorageOptions();
        return $"{options.ThumbnailsSubdirectory}/{document.Id:D7}.png";
    }

    /// <summary>
    /// Gets the relative storage path for the document preview.
    /// </summary>
    /// <param name="document">The document entity.</param>
    /// <returns>
    /// A relative path like "previews/0000001.pdf".
    /// </returns>
    public static string GetPreviewPath(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var options = new FileStorageOptions();
        return $"{options.PreviewsSubdirectory}/{document.Id:D7}.pdf";
    }

    /// <summary>
    /// Resolves which subdirectory a given relative storage path belongs to.
    /// Useful for determining the storage type of an arbitrary path.
    /// </summary>
    public static string GetSubdirectoryFromPath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var normalized = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var firstSeparator = normalized.IndexOf(Path.DirectorySeparatorChar);

        return firstSeparator > 0
            ? normalized[..firstSeparator]
            : normalized;
    }

    /// <summary>
    /// Gets the file extension (including the dot) for a document based on its type.
    /// </summary>
    private static string GetFileExtension(Document document)
    {
        // Default to .pdf if no filename extension is available
        if (document.Filename is null)
            return ".pdf";

        var ext = Path.GetExtension(document.Filename);
        return string.IsNullOrEmpty(ext) ? ".pdf" : ext;
    }
}
