namespace Paperless.Infrastructure.Caching;

/// <summary>
/// Configuration options for Redis distributed cache.
/// Bound from the "Redis" section in appsettings.json via IOptions.
/// </summary>
public class RedisOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Redis";

    /// <summary>
    /// The Redis connection string (e.g. "localhost:6379").
    /// Supports the StackExchange.Redis format including password, database,
    /// SSL, and timeout options.
    /// </summary>
    /// <remarks>
    /// Examples:
    ///   - "localhost:6379"
    ///   - "redis.example.com:6380,password=secret,ssl=true"
    ///   - "localhost:6379,defaultDatabase=1"
    /// </remarks>
    public string Configuration { get; set; } = "localhost:6379";

    /// <summary>
    /// The Redis instance name prefix used for cache keys.
    /// When set, all cache keys are prefixed with this value followed by ":".
    /// Useful for sharing a single Redis instance across multiple applications.
    /// </summary>
    public string InstanceName { get; set; } = "Paperless";

    /// <summary>
    /// Default sliding expiration time for cached items when no expiry
    /// is explicitly specified in the API call.
    /// </summary>
    public TimeSpan DefaultSlidingExpiration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Absolute expiration relative to now, applied in addition to sliding
    /// expiration as a safety bound. Defaults to 1 hour.
    /// </summary>
    public TimeSpan DefaultAbsoluteExpirationRelativeToNow { get; set; } = TimeSpan.FromHours(1);
}
