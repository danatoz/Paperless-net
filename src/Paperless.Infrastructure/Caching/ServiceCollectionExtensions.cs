using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Paperless.Infrastructure.Caching;

/// <summary>
/// Extension methods for registering Redis distributed cache services
/// with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures and registers Redis distributed cache.
    /// <list type="bullet">
    ///   <item>Binds <see cref="RedisOptions"/> from the <c>"Redis"</c> configuration section.</item>
    ///   <item>Registers <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/>
    ///     via <c>AddStackExchangeRedisCache</c>.</item>
    ///   <item>Registers <see cref="DistributedCacheService"/> as a transient service.</item>
    /// </list>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddRedisCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind RedisOptions from the "Redis" configuration section
        var section = configuration.GetSection(RedisOptions.SectionName);
        services.Configure<RedisOptions>(options => section.Bind(options));

        // Register StackExchangeRedis IDistributedCache implementation.
        // The Configuration and InstanceName are read from RedisOptions via
        // a configure callback that resolves from DI.
        services.AddStackExchangeRedisCache(options =>
        {
            // Resolve RedisOptions from the service provider at registration time
            // to get the bound configuration values.
            var sp = services.BuildServiceProvider();
            var redisOptions = sp.GetRequiredService<IOptions<RedisOptions>>().Value;

            options.Configuration = redisOptions.Configuration;
            options.InstanceName = redisOptions.InstanceName;
        });

        // Register the typed wrapper service
        services.AddTransient<DistributedCacheService>();

        return services;
    }
}
