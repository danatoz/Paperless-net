using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Paperless.Infrastructure.Queue;

/// <summary>
/// Configures Hangfire storage backend based on the application settings.
/// This encapsulates the storage provider logic (PostgreSQL / Memory / SQLite)
/// and is called from the ASP.NET Core host's DI configuration in the API project.
/// </summary>
public static class HangfireStorageConfigurator
{
    /// <summary>
    /// Configures Hangfire's storage and global settings using <see cref="HangfireOptions"/>.
    /// </summary>
    /// <param name="config">The Hangfire global configuration.</param>
    /// <param name="options">The Hangfire options with storage provider and connection details.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an unsupported storage provider is specified, or when PostgreSQL
    /// is selected without a connection string.
    /// </exception>
    public static void ConfigureStorage(IGlobalConfiguration config, HangfireOptions options)
    {
        switch (options.StorageProvider?.ToLowerInvariant())
        {
            case "postgresql":
            case "postgres":
                if (string.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    throw new InvalidOperationException(
                        "Hangfire PostgreSQL storage requires a non-empty ConnectionString. " +
                        "Configure it in the 'Hangfire:ConnectionString' setting or " +
                        "via the 'ConnectionStrings__Hangfire' environment variable.");
                }

                config.UsePostgreSqlStorage(
                    bootstrapperOpt => bootstrapperOpt.UseNpgsqlConnection(options.ConnectionString, null),
                    new PostgreSqlStorageOptions
                    {
                        // Use a dedicated schema to keep Hangfire tables separate
                        SchemaName = "hangfire",
                        // Create schema and tables on first use
                        PrepareSchemaIfNecessary = true
                    });
                break;

            case "memory":
                config.UseMemoryStorage(new MemoryStorageOptions
                {
                    FetchNextJobTimeout = TimeSpan.FromSeconds(5)
                });
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported Hangfire storage provider: '{options.StorageProvider}'. " +
                    $"Supported values: 'PostgreSQL' for production, 'Memory' for development/testing.");
        }
    }

    /// <summary>
    /// Binds <see cref="HangfireOptions"/> from the <c>"Hangfire"</c> configuration section.
    /// </summary>
    public static void BindOptions(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(HangfireOptions.SectionName);
        services.Configure<HangfireOptions>(options => section.Bind(options));
    }
}
