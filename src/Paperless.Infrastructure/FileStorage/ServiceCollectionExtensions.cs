using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Paperless.Core.Common.Interfaces;

namespace Paperless.Infrastructure.FileStorage;

/// <summary>
/// Extension methods for registering file storage services with the DI container.
/// Supports conditional registration: if S3Storage configuration is present and
/// has a bucket name configured, <see cref="S3FileStorage"/> is registered;
/// otherwise <see cref="LocalFileStorage"/> is used as the default.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the appropriate <see cref="IFileStorage"/> implementation based on
    /// the application configuration.
    ///
    /// <list type="bullet">
    ///   <item>
    ///     <description>If the <c>"S3Storage"</c> configuration section exists with a
    ///       non-empty <c>BucketName</c>, <see cref="S3FileStorage"/> is registered.</description>
    ///   </item>
    ///   <item>
    ///     <description>Otherwise, <see cref="LocalFileStorage"/> is registered as the default.</description>
    ///   </item>
    /// </list>
    ///
    /// Expected configuration (<c>appsettings.json</c>):
    /// <code>
    /// {
    ///   "S3Storage": {
    ///     "BucketName": "my-documents",
    ///     "Region": "us-east-1",
    ///     "AccessKey": "...",
    ///     "SecretKey": "...",
    ///     "Endpoint": "http://localhost:9000",   // optional, for MinIO etc.
    ///     "ForcePathStyle": true
    ///   }
    /// }
    /// </code>
    ///
    /// Default <see cref="FileStorageOptions"/> are used for LocalFileStorage unless
    /// configured via <c>services.Configure&lt;FileStorageOptions&gt;(...)</c> before
    /// calling this method.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">
    /// The application configuration root. Used to read the "S3Storage" section.
    /// </param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddFileStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var s3Section = configuration.GetSection(S3StorageOptions.SectionName);
        var bucketName = s3Section["BucketName"];

        if (s3Section.Exists() && !string.IsNullOrWhiteSpace(bucketName))
        {
            // S3 is configured — register S3FileStorage with S3 options
            services.Configure<S3StorageOptions>(s3Section);
            services.AddSingleton<IFileStorage, S3FileStorage>();
        }
        else
        {
            // No S3 configuration — fall back to local file storage
            services.AddSingleton<IFileStorage, LocalFileStorage>();
        }

        return services;
    }

    /// <summary>
    /// Registers <see cref="LocalFileStorage"/> as the <see cref="IFileStorage"/> implementation.
    /// Use this overload when you explicitly want local storage regardless of S3 configuration.
    ///
    /// Default <see cref="FileStorageOptions"/> are used unless configured via
    /// <c>services.Configure&lt;FileStorageOptions&gt;(...)</c> before calling this method.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddLocalFileStorage(this IServiceCollection services)
    {
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        return services;
    }
}
