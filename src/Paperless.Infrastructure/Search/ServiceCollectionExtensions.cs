using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Paperless.Core.Common.Interfaces;

namespace Paperless.Infrastructure.Search;

/// <summary>
/// Extension methods for registering Lucene.NET search services with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Lucene.NET search backend and index manager services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional delegate to configure <see cref="SearchIndexOptions"/>.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddLuceneSearch(
        this IServiceCollection services,
        Action<SearchIndexOptions>? configureOptions = null)
    {
        // Register configuration
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Register the index manager as a singleton (thread-safe, manages the Lucene directory)
        services.AddSingleton<SearchIndexManager>();

        // Register the search backend as a singleton
        services.AddSingleton<LuceneSearchBackend>();

        // Register the ISearchBackend interface
        services.AddSingleton<ISearchBackend>(sp => sp.GetRequiredService<LuceneSearchBackend>());

        // Register MediatR notification handlers for index synchronization
        services.AddTransient<INotificationHandler<Core.Documents.Events.DocumentCreatedEvent>,
            DocumentSearchIndexEventHandler>();

        services.AddTransient<INotificationHandler<Core.Documents.Events.DocumentUpdatedEvent>,
            DocumentSearchIndexEventHandler>();

        services.AddTransient<INotificationHandler<Core.Documents.Events.DocumentDeletedEvent>,
            DocumentSearchIndexEventHandler>();

        return services;
    }
}
