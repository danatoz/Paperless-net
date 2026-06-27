namespace Paperless.Infrastructure.Persistence;

/// <summary>
/// Configuration options for database provider selection and connection.
/// Bound from appsettings.json / environment variables via IOptions.
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Database";

    /// <summary>
    /// The database provider to use. Supported values: "PostgreSQL", "SQLite".
    /// </summary>
    public string Provider { get; set; } = "PostgreSQL";

    /// <summary>
    /// The connection string for the database.
    /// When using environment-specific configurations, this can be overridden
    /// via the ConnectionStrings__Default or Database:ConnectionString environment variable.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}
