using Hangfire;
using Microsoft.Extensions.Options;
using Paperless.Infrastructure.Queue;

namespace Paperless.Api.Extensions;

/// <summary>
/// Extension methods for configuring Hangfire background job services
/// in the ASP.NET Core DI container.
/// </summary>
public static class HangfireServiceCollectionExtensions
{
    /// <summary>
    /// Registers Hangfire storage, server, and global configuration.
    /// Storage provider and settings are read from the <c>"Hangfire"</c>
    /// configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddHangfireConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind HangfireOptions from configuration
        HangfireStorageConfigurator.BindOptions(services, configuration);

        // Configure Hangfire global storage and settings
        services.AddHangfire((sp, config) =>
        {
            var options = sp.GetRequiredService<IOptions<HangfireOptions>>().Value;

            // Configure storage backend (PostgreSQL / Memory)
            HangfireStorageConfigurator.ConfigureStorage(config, options);

            // Use JSON serialization (replaces signed-pickle from Celery)
            config.UseRecommendedSerializerSettings();

            // Set compatibility level
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
        });

        // Configure and register the Hangfire background job server
        services.AddHangfireServer((sp, options) =>
        {
            var hangfireOptions = sp.GetRequiredService<IOptions<HangfireOptions>>().Value;

            options.WorkerCount = hangfireOptions.WorkerCount;
            options.Queues = hangfireOptions.Queues;

            // Polling interval for scheduled jobs (15 seconds is a good default)
            options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}
