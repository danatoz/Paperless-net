using System.Net;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Paperless.Core.Common.Interfaces;

namespace Paperless.Infrastructure.FileStorage;

/// <summary>
/// Implementation of <see cref="IFileStorage"/> that stores files in an S3-compatible
/// object storage (AWS S3, MinIO, DigitalOcean Spaces, Wasabi, etc.).
///
/// Paths passed to IFileStorage methods are treated as S3 object keys.
/// All operations target a single bucket configured via <see cref="S3StorageOptions"/>.
/// </summary>
public class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3StorageOptions _options;
    private readonly ILogger<S3FileStorage> _logger;

    /// <summary>
    /// Initializes a new instance using DI-provided options and logger.
    /// Creates the <see cref="AmazonS3Client"/> from the given configuration.
    /// </summary>
    public S3FileStorage(IOptions<S3StorageOptions> options, ILogger<S3FileStorage> logger)
        : this(CreateS3Client(options.Value), options.Value, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance with an externally provided S3 client.
    /// Useful for testing with mocked <see cref="IAmazonS3"/>.
    /// </summary>
    public S3FileStorage(IAmazonS3 s3Client, S3StorageOptions options, ILogger<S3FileStorage> logger)
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.BucketName))
        {
            throw new InvalidOperationException(
                "S3 bucket name must be configured in the S3Storage section.");
        }
    }

    /// <inheritdoc />
    public async Task StoreAsync(Stream stream, string path, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var key = NormalizeKey(path);
        _logger.LogDebug("Storing file to S3: bucket={Bucket}, key={Key}", _options.BucketName, key);

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = stream,
            AutoCloseStream = false,
        };

        await _s3Client.PutObjectAsync(request, ct);
    }

    /// <inheritdoc />
    public async Task<Stream> ReadAsync(string path, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var key = NormalizeKey(path);
        _logger.LogDebug("Reading file from S3: bucket={Bucket}, key={Key}", _options.BucketName, key);

        var request = new GetObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
        };

        try
        {
            var response = await _s3Client.GetObjectAsync(request, ct);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "File not found in S3: bucket={Bucket}, key={Key}", _options.BucketName, key);
            throw new FileNotFoundException($"File not found at path: {path}", path);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string path, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var key = NormalizeKey(path);
        _logger.LogDebug("Deleting file from S3: bucket={Bucket}, key={Key}", _options.BucketName, key);

        var request = new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
        };

        await _s3Client.DeleteObjectAsync(request, ct);
    }

    /// <inheritdoc />
    public async Task MoveAsync(string sourcePath, string destPath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destPath);

        var sourceKey = NormalizeKey(sourcePath);
        var destKey = NormalizeKey(destPath);

        _logger.LogDebug(
            "Moving file in S3: bucket={Bucket}, source={Source}, dest={Dest}",
            _options.BucketName, sourceKey, destKey);

        // Copy source to destination within the same bucket
        var copyRequest = new CopyObjectRequest
        {
            SourceBucket = _options.BucketName,
            SourceKey = sourceKey,
            DestinationBucket = _options.BucketName,
            DestinationKey = destKey,
        };

        try
        {
            await _s3Client.CopyObjectAsync(copyRequest, ct);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex,
                "Source file not found for move in S3: bucket={Bucket}, key={Key}",
                _options.BucketName, sourceKey);
            throw new FileNotFoundException($"Source file not found: {sourcePath}", sourcePath);
        }

        // Delete the source after successful copy
        await DeleteAsync(sourcePath, ct);

        _logger.LogDebug(
            "Move completed in S3: bucket={Bucket}, source={Source}, dest={Dest}",
            _options.BucketName, sourceKey, destKey);
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
    /// Normalizes path separators to S3-style forward slashes.
    /// </summary>
    private static string NormalizeKey(string path)
    {
        // S3 uses forward slashes as path separators regardless of OS
        return path.Replace('\\', '/');
    }

    /// <summary>
    /// Creates an <see cref="IAmazonS3"/> client from the given options.
    /// If access/secret keys are provided, uses them; otherwise falls back
    /// to the default credential chain (IAM roles, environment variables, etc.).
    /// </summary>
    private static IAmazonS3 CreateS3Client(S3StorageOptions options)
    {
        var config = new AmazonS3Config
        {
            ForcePathStyle = options.ForcePathStyle,
        };

        if (!string.IsNullOrWhiteSpace(options.Endpoint))
        {
            config.ServiceURL = options.Endpoint;
        }
        else if (!string.IsNullOrWhiteSpace(options.Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
        }

        if (!string.IsNullOrWhiteSpace(options.AccessKey) &&
            !string.IsNullOrWhiteSpace(options.SecretKey))
        {
            var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
            return new AmazonS3Client(credentials, config);
        }

        // Fall back to the default credential chain
        return new AmazonS3Client(config);
    }
}
