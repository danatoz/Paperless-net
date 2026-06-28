using FluentAssertions;
using Paperless.Infrastructure.Tests.Infrastructure;

namespace Paperless.Infrastructure.Tests.Basic;

/// <summary>
/// Basic integration tests verifying that the database connection
/// is established correctly through the <see cref="DbFixture"/>.
/// </summary>
[Trait("Category", TestCategories.Integration)]
public class DbConnectionTests : IClassFixture<DbFixture>
{
    private readonly DbFixture _fixture;

    public DbConnectionTests(DbFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies that a DbContext can be created and the database
    /// connection can be opened successfully.
    /// </summary>
    [Fact]
    public async Task Can_Open_Connection()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var canConnect = await context.Database.CanConnectAsync();

        // Assert
        canConnect.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that multiple contexts can use the same in-memory
    /// database connection concurrently.
    /// </summary>
    [Fact]
    public async Task Multiple_Contexts_Share_Same_Database()
    {
        // Arrange
        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        // Act & Assert
        var canConnect1 = await context1.Database.CanConnectAsync();
        var canConnect2 = await context2.Database.CanConnectAsync();

        canConnect1.Should().BeTrue();
        canConnect2.Should().BeTrue();
    }
}
