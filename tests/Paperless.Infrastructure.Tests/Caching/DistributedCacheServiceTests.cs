using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Paperless.Infrastructure.Caching;

namespace Paperless.Infrastructure.Tests.Caching;

/// <summary>
/// Unit tests for <see cref="DistributedCacheService"/>.
/// Uses NSubstitute to mock the underlying <see cref="IDistributedCache"/>.
/// </summary>
public class DistributedCacheServiceTests
{
    private readonly IDistributedCache _innerCache;
    private readonly DistributedCacheService _service;
    private readonly RedisOptions _options;
    private readonly ILogger<DistributedCacheService> _logger;

    public DistributedCacheServiceTests()
    {
        _innerCache = Substitute.For<IDistributedCache>();
        _options = new RedisOptions
        {
            Configuration = "localhost:6379",
            InstanceName = "Test",
            DefaultSlidingExpiration = TimeSpan.FromMinutes(5),
            DefaultAbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };
        _logger = Substitute.For<ILogger<DistributedCacheService>>();

        _service = new DistributedCacheService(
            _innerCache,
            Options.Create(_options),
            _logger);
    }

    // ── GetOrCreateAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetOrCreateAsync_Should_ReturnCachedValue_WhenCacheHit()
    {
        // Arrange
        var key = "test-key";
        var expected = new TestData { Id = 1, Name = "Cached" };
        var serialized = JsonSerializer.SerializeToUtf8Bytes(expected, _jsonOptions);

        _innerCache.GetAsync(key, Arg.Any<CancellationToken>())
            .Returns(serialized);

        var factoryCalled = false;

        // Act
        var result = await _service.GetOrCreateAsync(key, () =>
        {
            factoryCalled = true;
            return Task.FromResult(new TestData { Id = 2, Name = "Factory" });
        });

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Cached");
        factoryCalled.Should().BeFalse("the factory should not be called on cache hit");
    }

    [Fact]
    public async Task GetOrCreateAsync_Should_ExecuteFactory_WhenCacheMiss()
    {
        // Arrange
        var key = "miss-key";
        _innerCache.GetAsync(key, Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        var factoryValue = new TestData { Id = 42, Name = "FromFactory" };

        // Act
        var result = await _service.GetOrCreateAsync(key, () => Task.FromResult(factoryValue));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.Name.Should().Be("FromFactory");

        // Verify the value was stored
        await _innerCache.Received(1).SetAsync(
            key,
            Arg.Is<byte[]>(b => Deserialize<TestData>(b)!.Id == 42),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrCreateAsync_Should_NotCache_WhenExpiryIsZero()
    {
        // Arrange
        var key = "zero-expiry";
        _innerCache.GetAsync(key, Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        // Act
        var result = await _service.GetOrCreateAsync(
            key,
            () => Task.FromResult(new TestData { Id = 99, Name = "NoCache" }),
            expiry: TimeSpan.Zero);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(99);

        // Verify SetAsync was NOT called
        await _innerCache.DidNotReceiveWithAnyArgs().SetAsync(
            default!, default!, default!, default);
    }

    [Fact]
    public async Task GetOrCreateAsync_Should_ReExecuteFactory_WhenDeserializationFails()
    {
        // Arrange
        var key = "corrupt-key";
        var corruptBytes = Encoding.UTF8.GetBytes("this is not valid JSON");
        _innerCache.GetAsync(key, Arg.Any<CancellationToken>())
            .Returns(corruptBytes);

        var factoryValue = new TestData { Id = 77, Name = "Recovered" };

        // Act
        var result = await _service.GetOrCreateAsync(key, () => Task.FromResult(factoryValue));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(77);
        result.Name.Should().Be("Recovered");
    }

    [Fact]
    public async Task GetOrCreateAsync_Should_Throw_WhenKeyIsNull()
    {
        // Act
        var act = () => _service.GetOrCreateAsync<string>(null!, () => Task.FromResult("value"));

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetOrCreateAsync_Should_Throw_WhenFactoryIsNull()
    {
        // Act
        var act = () => _service.GetOrCreateAsync<string>("key", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── GetAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_Should_ReturnNull_WhenKeyDoesNotExist()
    {
        // Arrange
        _innerCache.GetAsync("missing", Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        // Act
        var result = await _service.GetAsync<TestData>("missing");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_Should_ReturnDeserializedValue_WhenKeyExists()
    {
        // Arrange
        var expected = new TestData { Id = 10, Name = "Found" };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(expected, _jsonOptions);
        _innerCache.GetAsync("exists", Arg.Any<CancellationToken>())
            .Returns(bytes);

        // Act
        var result = await _service.GetAsync<TestData>("exists");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(10);
        result.Name.Should().Be("Found");
    }

    [Fact]
    public async Task GetAsync_Should_ReturnNull_WhenDeserializationFails()
    {
        // Arrange
        var corruptBytes = Encoding.UTF8.GetBytes("{bad json}");
        _innerCache.GetAsync("bad", Arg.Any<CancellationToken>())
            .Returns(corruptBytes);

        // Act
        var result = await _service.GetAsync<TestData>("bad");

        // Assert
        result.Should().BeNull();
    }

    // ── SetAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task SetAsync_Should_SerializeAndStoreValue()
    {
        // Arrange
        var key = "set-key";
        var value = new TestData { Id = 5, Name = "Set" };

        // Act
        await _service.SetAsync(key, value);

        // Assert
        await _innerCache.Received(1).SetAsync(
            key,
            Arg.Is<byte[]>(b => Deserialize<TestData>(b)!.Id == 5),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsync_Should_Throw_WhenKeyIsNull()
    {
        // Act
        var act = () => _service.SetAsync(null!, "value");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── RemoveAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task RemoveAsync_Should_RemoveKey()
    {
        // Act
        await _service.RemoveAsync("remove-key");

        // Assert
        await _innerCache.Received(1).RemoveAsync("remove-key", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_Should_Throw_WhenKeyIsNull()
    {
        // Act
        var act = () => _service.RemoveAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── RefreshAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_Should_RefreshKey()
    {
        // Act
        await _service.RefreshAsync("refresh-key");

        // Assert
        await _innerCache.Received(1).RefreshAsync("refresh-key", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_Should_Throw_WhenKeyIsNull()
    {
        // Act
        var act = () => _service.RefreshAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static T? Deserialize<T>(byte[] bytes)
    {
        return JsonSerializer.Deserialize<T>(bytes, _jsonOptions);
    }

    /// <summary>
    /// Simple record type used in test scenarios that require serialization.
    /// </summary>
    private record TestData
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}
