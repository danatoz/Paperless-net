using Microsoft.Extensions.Configuration;
using Serilog;

namespace Paperless.Infrastructure.Logging;

/// <summary>
/// Configures Serilog's <see cref="LoggerConfiguration"/> for the application.
///
/// <para>
/// Reads sinks, minimum-level overrides, and enrichers from the <c>"Serilog"</c>
/// configuration section via <c>ReadFrom.Configuration()</c>, then applies
/// programmatic enrichers (MachineName, EnvironmentName, ThreadId) that rely
/// on assemblies registered in the project.
/// </para>
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Applies Serilog configuration on top of the provided <paramref name="loggerConfiguration"/>.
    /// </summary>
    /// <param name="loggerConfiguration">The Serilog configuration builder.</param>
    /// <param name="configuration">The application root <see cref="IConfiguration"/>.</param>
    /// <returns>The same <paramref name="loggerConfiguration"/> for chaining.</returns>
    public static LoggerConfiguration Configure(
        LoggerConfiguration loggerConfiguration,
        IConfiguration configuration)
    {
        // Read sinks, minimum levels, and enrichers from the "Serilog" config section.
        // This enables operators to change logging behaviour at deployment time
        // without recompiling — matching the original Python logging model.
        loggerConfiguration.ReadFrom.Configuration(configuration);

        // Programmatic enrichers (these are always applied regardless of config).
        // WithMachineName / WithEnvironmentName come from Serilog.Enrichers.Environment.
        // WithThreadId comes from Serilog.Enrichers.Thread.
        loggerConfiguration
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId();

        return loggerConfiguration;
    }
}
