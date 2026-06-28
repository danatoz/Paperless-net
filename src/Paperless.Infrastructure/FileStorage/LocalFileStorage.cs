using Microsoft.Extensions.Options;
using Paperless.Core.Common.Interfaces;

namespace Paperless.Infrastructure.FileStorage;

/// <summary>
/// Implementation of <see cref="IFileStorage"/> that stores files on the local filesystem.
/// Provides atomic write operations (write to temp file, then atomically rename)
/// and automatic directory creation. All paths passed to IFileStorage methods are
/// treated as relative to <see cref="FileStorageOptions.BaseDirectory"/>.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly FileStorageOptions _options;

    public LocalFileStorage(IOptions<FileStorageOptions> options)
    {
        _options = options.Value;

        // Ensure all storage directories exist at startup
        Directory.CreateDirectory(_options.OriginalsDirectory);
        Directory.CreateDirectory(_options.ArchiveDirectory);
        Directory.CreateDirectory(_options.ThumbnailsDirectory);
        Directory.CreateDirectory(_options.PreviewsDirectory);
    }

    /// <inheritdoc />
    public async Task StoreAsync(Stream stream, string path, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = ResolveFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);

        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        // Write to a temporary file first, then atomically move to the target.
        // This prevents partial writes if the process crashes mid-write.
        var tempPath = $"{fullPath}.tmp.{Guid.NewGuid():N}";
        try
        {
            await using (var tempStream = new FileStream(
                             tempPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 4096,
                             useAsync: true))
            {
                await stream.CopyToAsync(tempStream, ct);
                await tempStream.FlushAsync(ct);
            }

            // Atomic rename (overwrite if the target already exists)
            File.Move(tempPath, fullPath, overwrite: true);
        }
        catch
        {
            // Clean up the temp file if anything went wrong
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

    /// <inheritdoc />
    public Task<Stream> ReadAsync(string path, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = ResolveFullPath(path);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"File not found at path: {path}",
                fullPath);
        }

        // Use FileShare.Read to allow concurrent reads
        var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        return Task.FromResult<Stream>(stream);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = ResolveFullPath(path);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task MoveAsync(string sourcePath, string destPath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destPath);

        var fullSourcePath = ResolveFullPath(sourcePath);
        var fullDestPath = ResolveFullPath(destPath);

        if (!File.Exists(fullSourcePath))
        {
            throw new FileNotFoundException(
                $"Source file not found: {sourcePath}",
                fullSourcePath);
        }

        var destDirectory = Path.GetDirectoryName(fullDestPath);
        if (destDirectory is not null)
        {
            Directory.CreateDirectory(destDirectory);
        }

        // Overwrite the destination if it already exists
        if (File.Exists(fullDestPath))
        {
            File.Delete(fullDestPath);
        }

        File.Move(fullSourcePath, fullDestPath);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Stream> GetThumbnailAsync(string path, CancellationToken ct = default)
    {
        // Thumbnails are stored as regular files — delegate to ReadAsync
        return ReadAsync(path, ct);
    }

    /// <inheritdoc />
    public Task<Stream> GetPreviewAsync(string path, CancellationToken ct = default)
    {
        // Previews are stored as regular files — delegate to ReadAsync
        return ReadAsync(path, ct);
    }

    /// <summary>
    /// Resolves a relative storage path to an absolute filesystem path
    /// rooted at <see cref="FileStorageOptions.BaseDirectory"/>.
    /// </summary>
    private string ResolveFullPath(string relativePath)
    {
        // Normalize path separators and combine with base directory
        var normalizedPath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(_options.BaseDirectory, normalizedPath));
    }
}
