namespace Paperless.Core.Common.Interfaces;

/// <summary>
/// Abstraction for file storage operations (local filesystem, S3, Azure Blob, etc.).
/// This is a new abstraction not present in the original paperless-ngx codebase,
/// needed for testability and cloud storage support.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Stores a file from the given stream to the specified path.
    /// </summary>
    /// <param name="stream">The stream containing the file data.</param>
    /// <param name="path">The destination path (relative to the storage root).</param>
    /// <param name="ct">Cancellation token.</param>
    Task StoreAsync(Stream stream, string path, CancellationToken ct = default);

    /// <summary>
    /// Reads a file from the specified path and returns its stream.
    /// </summary>
    Task<Stream> ReadAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Deletes a file at the specified path.
    /// </summary>
    Task DeleteAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Moves a file from source to destination path.
    /// </summary>
    Task MoveAsync(string sourcePath, string destPath, CancellationToken ct = default);

    /// <summary>
    /// Gets the thumbnail stream for a document.
    /// </summary>
    Task<Stream> GetThumbnailAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Gets the preview stream for a document.
    /// </summary>
    Task<Stream> GetPreviewAsync(string path, CancellationToken ct = default);
}
