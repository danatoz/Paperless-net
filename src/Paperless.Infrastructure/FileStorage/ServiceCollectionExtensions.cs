using Microsoft.Extensions.DependencyInjection;
using Paperless.Core.Common.Interfaces;

namespace Paperless.Infrastructure.FileStorage;

/// <summary>
/// Extension methods for registering file storage services with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="LocalFileStorage"/> as the <see cref="IFileStorage"/> implementation.
    /// Default <see cref="FileStorageOptions"/> are used unless configured via
    /// <c>services.Configure&lt;FileStorageOptions&gt;(...)</c> before calling this method.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddFileStorage(this IServiceCollection services)
    {
        // Register LocalFileStorage as the IFileStorage implementation.
        // Singleton is safe: it only reads options at construction time and
        // creates the required storage directories.
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        return services;
    }
}
