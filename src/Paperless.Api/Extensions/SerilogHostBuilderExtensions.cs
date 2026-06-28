using Paperless.Infrastructure.Logging;
using Serilog;

namespace Paperless.Api.Extensions;

/// <summary>
/// Extension methods for configuring the ASP.NET Core host with Serilog
/// as the primary logging provider, replacing the default <c>ILogger&lt;T&gt;</c>
/// implementation.
/// </summary>
public static class SerilogHostBuilderExtensions
{
    /// <summary>
    /// Configures the host builder to use Serilog with settings from
    /// <c>appsettings.json</c> and additional programmatic enrichers.
    ///
    /// <para>
    /// Call this in <c>Program.cs</c> right after <c>CreateBuilder()</c>:
    /// <code>
    ///   builder.Host.UseSerilogWithConfiguration();
    /// </code>
    /// </para>
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <returns>The same host builder for chaining.</returns>
    public static IHostBuilder UseSerilogWithConfiguration(this IHostBuilder hostBuilder)
    {
        // UseSerilog is called with a configure callback so that:
        //   1. IConfiguration (from appsettings.json + env vars) is available
        //   2. Serilog's ReadFrom.Configuration picks up the "Serilog" section
        //   3. Programmatic enrichers (MachineName, EnvironmentName, ThreadId)
        //      are applied after the config-based setup
        hostBuilder.UseSerilog((context, _, loggerConfiguration) =>
        {
            LoggingConfiguration.Configure(loggerConfiguration, context.Configuration);
        });

        return hostBuilder;
    }
}
