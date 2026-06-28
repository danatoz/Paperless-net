using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Paperless.Infrastructure.Caching;

/// <summary>
/// Typed wrapper over <see cref="IDistributedCache"/> that provides
/// serialization/deserialization via System.Text.Json and a
/// convenience <see cref="GetOrCreateAsync{T}"/> pattern.
/// </summary>
public class DistributedCacheService
{
    private readonly IDistributedCache _cache;
    private readonly RedisOptions _options;
    private readonly ILogger<DistributedCacheService> _logger;

    /// <summary>
    /// JSON serializer options: case-insensitive property matching,
    /// camelCase naming (matching typical JSON API conventions).
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DistributedCacheService(
        IDistributedCache cache,
        IOptions<RedisOptions> options,
        ILogger<DistributedCacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a value from the cache. If it does not exist, the <paramref name="factory"/>
    /// is executed, its result is serialized and stored in the cache, and then returned.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">
    /// An asynchronous factory function that produces the value to cache when a cache miss occurs.
    /// </param>
    /// <param name="expiry">
    /// Optional sliding expiration for the cached entry.
    /// If <c>null</c>, the <see cref="RedisOptions.DefaultSlidingExpiration"/> is used.
    /// Pass <see cref="TimeSpan.Zero"/> to avoid caching the result (execute factory every time).
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The cached or freshly created value.</returns>
    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        // Attempt to read from cache
        var cachedBytes = await _cache.GetAsync(key, cancellationToken);

        if (cachedBytes is not null && cachedBytes.Length > 0)
        {
            try
            {
                var result = JsonSerializer.Deserialize<T>(cachedBytes, JsonOptions);
                if (result is not null)
                {
                    _logger.LogTrace("Cache hit for key '{Key}'", key);
                    return result;
                }

                _logger.LogWarning(
                    "Cache entry for key '{Key}' contained null after deserialization; re-executing factory.",
                    key);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to deserialize cache entry for key '{Key}'; re-executing factory.",
                    key);
            }
        }

        // Cache miss — execute factory
        _logger.LogTrace("Cache miss for key '{Key}'", key);
        var value = await factory();

        // Store in cache (skip if expiry is explicitly TimeSpan.Zero)
        if (expiry is null || expiry.Value != TimeSpan.Zero)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = expiry ?? _options.DefaultSlidingExpiration,
                AbsoluteExpirationRelativeToNow = _options.DefaultAbsoluteExpirationRelativeToNow
            };

            await _cache.SetAsync(key, bytes, options, cancellationToken);
            _logger.LogTrace("Cached value for key '{Key}'", key);
        }

        return value;
    }

    /// <summary>
    /// Retrieves a typed value from the cache.
    /// Returns <c>null</c> if the key does not exist or deserialization fails.
    /// </summary>
    public async Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var cachedBytes = await _cache.GetAsync(key, cancellationToken);
        if (cachedBytes is null || cachedBytes.Length == 0)
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(cachedBytes, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize cache entry for key '{Key}'", key);
            return default;
        }
    }

    /// <summary>
    /// Sets a typed value in the cache with the specified expiration.
    /// </summary>
    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = expiry ?? _options.DefaultSlidingExpiration,
            AbsoluteExpirationRelativeToNow = _options.DefaultAbsoluteExpirationRelativeToNow
        };

        await _cache.SetAsync(key, bytes, options, cancellationToken);
        _logger.LogTrace("Set cache entry for key '{Key}'", key);
    }

    /// <summary>
    /// Removes a value from the cache by key.
    /// </summary>
    public Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        _logger.LogTrace("Removed cache entry for key '{Key}'", key);
        return _cache.RemoveAsync(key, cancellationToken);
    }

    /// <summary>
    /// Refreshes the sliding expiration of a cache entry, extending its lifetime.
    /// </summary>
    public Task RefreshAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _cache.RefreshAsync(key, cancellationToken);
    }
}
