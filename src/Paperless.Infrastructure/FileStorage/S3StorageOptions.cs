namespace Paperless.Infrastructure.FileStorage;

/// <summary>
/// Configuration options for S3-compatible file storage.
/// Maps to the "S3Storage" section in appsettings.json.
/// Supports AWS S3, MinIO, DigitalOcean Spaces, Wasabi, and any
/// S3-compatible storage provider.
/// </summary>
public class S3StorageOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "S3Storage";

    /// <summary>
    /// The S3 bucket name where files will be stored.
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// AWS region (e.g., "us-east-1"). Required for AWS S3.
    /// For S3-compatible storage (MinIO, etc.), use <see cref="Endpoint"/> instead.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// AWS access key ID. If not set, falls back to the default credential chain
    /// (IAM role, environment variables, etc.).
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// AWS secret access key.
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Custom endpoint URL for S3-compatible storage (MinIO, DigitalOcean Spaces, etc.).
    /// Example: "http://localhost:9000" for MinIO.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// When true, uses path-style addressing (bucket in path) instead of
    /// virtual-hosted-style (bucket in hostname). Required for MinIO.
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>
    /// Returns true when required S3 configuration is present.
    /// A bucket name is always required; credentials or default chain must be available.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(BucketName);
}
