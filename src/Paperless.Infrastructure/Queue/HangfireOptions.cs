namespace Paperless.Infrastructure.Queue;

/// <summary>
/// Configuration options for Hangfire background job processing.
/// Bound from the "Hangfire" section in appsettings.json via IOptions.
/// </summary>
public class HangfireOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Hangfire";

    /// <summary>
    /// The storage provider to use.
    /// Supported values: "PostgreSQL" (production), "Memory" (development/testing).
    /// </summary>
    public string StorageProvider { get; set; } = "PostgreSQL";

    /// <summary>
    /// Connection string for the PostgreSQL Hangfire storage.
    /// Required when <see cref="StorageProvider"/> is "PostgreSQL".
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Number of worker threads for processing background jobs.
    /// Default: CPU count × 5, recommended range: 3–20 per queue.
    /// </summary>
    public int WorkerCount { get; set; } = Environment.ProcessorCount * 5;

    /// <summary>
    /// Named queues that this server will process.
    /// Default: ["default"]. Custom queues: "mail", "ai", "bulk", "system".
    /// </summary>
    public string[] Queues { get; set; } = ["default"];

    /// <summary>
    /// Number of automatic retry attempts for failed jobs.
    /// Default: 10 (matches Celery default).
    /// Set to 0 to disable automatic retries.
    /// </summary>
    public int RetryCount { get; set; } = 10;

    /// <summary>
    /// Whether to enable the Hangfire Dashboard endpoint.
    /// Disable in production if not needed.
    /// </summary>
    public bool EnableDashboard { get; set; } = true;

    /// <summary>
    /// URL path for the Hangfire Dashboard.
    /// Default: "/hangfire".
    /// </summary>
    public string DashboardPath { get; set; } = "/hangfire";
}
