using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Paperless.Infrastructure.Queue;

namespace Paperless.Infrastructure.Tests.Queue;

/// <summary>
/// Tests for Hangfire configuration and integration.
/// Uses Memory storage to avoid external dependencies.
/// </summary>
public class HangfireConfigurationTests
{
    [Fact]
    public void HangfireStorageConfigurator_BindOptions_Should_PopulateFromConfiguration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:StorageProvider"] = "Memory",
                ["Hangfire:WorkerCount"] = "3",
                ["Hangfire:Queues:0"] = "custom-queue",
                ["Hangfire:RetryCount"] = "5",
                ["Hangfire:DashboardPath"] = "/jobs"
            }!)
            .Build();

        var services = new ServiceCollection();
        HangfireStorageConfigurator.BindOptions(services, configuration);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HangfireOptions>>().Value;

        // Assert
        options.StorageProvider.Should().Be("Memory");
        options.WorkerCount.Should().Be(3);
        options.Queues.Should().Contain("custom-queue");
        options.RetryCount.Should().Be(5);
        options.DashboardPath.Should().Be("/jobs");
    }

    [Fact]
    public void HangfireStorageConfigurator_BindOptions_Should_UseDefaults_WhenConfigMissing()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        HangfireStorageConfigurator.BindOptions(services, configuration);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HangfireOptions>>().Value;

        // Assert - defaults from HangfireOptions class
        options.StorageProvider.Should().Be("PostgreSQL");
        options.WorkerCount.Should().BeGreaterThan(0);
        options.Queues.Should().Contain("default");
        options.RetryCount.Should().Be(10);
        options.EnableDashboard.Should().BeTrue();
    }

    [Fact]
    public void ConfigureStorage_Should_ConfigureMemoryStorage_WhenMemoryProvider()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:StorageProvider"] = "Memory"
            }!)
            .Build();

        var services = new ServiceCollection();
        HangfireStorageConfigurator.BindOptions(services, config);

        services.AddHangfire((sp, hfConfig) =>
        {
            var options = sp.GetRequiredService<IOptions<HangfireOptions>>().Value;
            HangfireStorageConfigurator.ConfigureStorage(hfConfig, options);
        });

        using var sp = services.BuildServiceProvider();

        // Assert - verify no exceptions; Hangfire successfully configured
        var storage = sp.GetRequiredService<JobStorage>();
        storage.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureStorage_Should_Throw_WhenPostgresWithNoConnectionString()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:StorageProvider"] = "PostgreSQL",
                ["Hangfire:ConnectionString"] = ""
            }!)
            .Build();

        var services = new ServiceCollection();
        HangfireStorageConfigurator.BindOptions(services, configuration);

        services.AddHangfire((sp, hfConfig) =>
        {
            var options = sp.GetRequiredService<IOptions<HangfireOptions>>().Value;
            HangfireStorageConfigurator.ConfigureStorage(hfConfig, options);
        });

        using var sp = services.BuildServiceProvider();

        // Act - Hangfire configuration is lazy; accessing JobStorage triggers it
        var act = () => sp.GetRequiredService<JobStorage>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionString*");
    }

    [Fact]
    public void ConfigureStorage_Should_Throw_WhenUnsupportedProvider()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:StorageProvider"] = "MongoDB",
                ["Hangfire:ConnectionString"] = "mongodb://localhost"
            }!)
            .Build();

        var services = new ServiceCollection();
        HangfireStorageConfigurator.BindOptions(services, configuration);

        services.AddHangfire((sp, hfConfig) =>
        {
            var options = sp.GetRequiredService<IOptions<HangfireOptions>>().Value;
            HangfireStorageConfigurator.ConfigureStorage(hfConfig, options);
        });

        using var sp = services.BuildServiceProvider();

        // Act - Hangfire configuration is lazy; accessing JobStorage triggers it
        var act = () => sp.GetRequiredService<JobStorage>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MongoDB*");
    }

    [Fact]
    public void AddHangfireConfiguration_Should_RegisterServices_WhenMemoryStorage()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:StorageProvider"] = "Memory",
                ["Hangfire:WorkerCount"] = "1",
                ["Hangfire:EnableDashboard"] = "false"
            }!)
            .Build();

        var services = new ServiceCollection();

        // We reference the API extension directly by duplicating the registration pattern.
        // (This test validates that the full Hangfire DI setup works end-to-end.)
        HangfireStorageConfigurator.BindOptions(services, configuration);
        services.AddHangfire((sp, hfConfig) =>
        {
            var options = sp.GetRequiredService<IOptions<HangfireOptions>>().Value;
            HangfireStorageConfigurator.ConfigureStorage(hfConfig, options);
            hfConfig.UseRecommendedSerializerSettings();
            hfConfig.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
        });

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 1;
            options.Queues = ["default"];
        });

        using var sp = services.BuildServiceProvider();

        // Assert - core Hangfire services are registered
        sp.GetService<IBackgroundJobClient>().Should().NotBeNull();
        sp.GetService<JobStorage>().Should().NotBeNull();
        sp.GetService<IRecurringJobManager>().Should().NotBeNull();
    }

    [Fact]
    public void EnqueueJob_Should_CreateJob_WithMemoryStorage()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:StorageProvider"] = "Memory",
                ["Hangfire:WorkerCount"] = "1",
                ["Hangfire:EnableDashboard"] = "false"
            }!)
            .Build();

        var services = new ServiceCollection();
        HangfireStorageConfigurator.BindOptions(services, configuration);
        services.AddHangfire((sp, hfConfig) =>
        {
            var options = sp.GetRequiredService<IOptions<HangfireOptions>>().Value;
            HangfireStorageConfigurator.ConfigureStorage(hfConfig, options);
            hfConfig.UseRecommendedSerializerSettings();
            hfConfig.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
        });
        services.AddHangfireServer(o => { o.WorkerCount = 1; });

        using var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<IBackgroundJobClient>();

        // Act
        var jobId = client.Enqueue(() => Console.WriteLine("Test job executed"));

        // Assert
        jobId.Should().NotBeNullOrEmpty("a valid job ID should be returned after enqueue");
    }

    [Fact]
    public void HangfireOptions_Should_HaveReasonableDefaults()
    {
        // Arrange
        var options = new HangfireOptions();

        // Assert
        options.StorageProvider.Should().Be("PostgreSQL", "production default should be PostgreSQL");
        options.WorkerCount.Should().BeGreaterThanOrEqualTo(1, "must have at least one worker");
        options.Queues.Should().NotBeEmpty("at least the default queue must be configured");
        options.RetryCount.Should().BeGreaterThanOrEqualTo(0);
        options.EnableDashboard.Should().BeTrue("Dashboard should be enabled by default for development");
        options.DashboardPath.Should().Be("/hangfire");
    }
}
