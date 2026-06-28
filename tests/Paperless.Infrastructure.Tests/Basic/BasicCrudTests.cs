using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Paperless.Core.Documents.Entities;
using Paperless.Infrastructure.Tests.Infrastructure;

namespace Paperless.Infrastructure.Tests.Basic;

/// <summary>
/// Basic integration tests verifying Create, Read, Update, and Delete
/// operations on a single entity (<see cref="Correspondent"/>).
/// </summary>
[Trait("Category", TestCategories.Integration)]
public class BasicCrudTests : IntegrationTestBase
{
    public BasicCrudTests(DbFixture fixture) : base(fixture)
    {
    }

    /// <summary>
    /// Verifies that a new entity can be created and persisted to the database.
    /// </summary>
    [Fact]
    public async Task Create_Entity_Persists_To_Database()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        var entity = new Correspondent
        {
            Name = "Test Correspondent",
            MatchingAlgorithm = Core.Documents.Enums.MatchingAlgorithm.Auto
        };

        // Act
        context.Correspondents.Add(entity);
        await context.SaveChangesAsync();

        // Assert
        entity.Id.Should().BeGreaterThan(0);
        var saved = await context.Correspondents.FindAsync(entity.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Test Correspondent");
    }

    /// <summary>
    /// Verifies that an entity can be read back from the database.
    /// </summary>
    [Fact]
    public async Task Read_Entity_From_Database()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        var entity = new Correspondent
        {
            Name = "Read Test",
            MatchingAlgorithm = Core.Documents.Enums.MatchingAlgorithm.Auto
        };
        context.Correspondents.Add(entity);
        await context.SaveChangesAsync();
        var createdId = entity.Id;

        // Act
        var retrieved = await context.Correspondents.FindAsync(createdId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Read Test");
    }

    /// <summary>
    /// Verifies that an entity can be updated in the database.
    /// </summary>
    [Fact]
    public async Task Update_Entity_In_Database()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        var entity = new Correspondent
        {
            Name = "Original Name",
            MatchingAlgorithm = Core.Documents.Enums.MatchingAlgorithm.Auto
        };
        context.Correspondents.Add(entity);
        await context.SaveChangesAsync();

        // Act
        entity.Name = "Updated Name";
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.Correspondents.FindAsync(entity.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
    }

    /// <summary>
    /// Verifies that an entity can be soft-deleted (IsDeleted flag set).
    /// <para>
    /// Note: <see cref="DbSet{T}.FindAsync"/> does NOT apply global query filters
    /// in EF Core, so the soft-deleted entity is still found by primary key.
    /// The soft-delete filter applies to LINQ queries like <see cref="EntityFrameworkQueryableExtensions.FirstOrDefaultAsync{T}"/>.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Delete_Entity_SoftDeletes()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        var entity = new Correspondent
        {
            Name = "To Delete",
            MatchingAlgorithm = Core.Documents.Enums.MatchingAlgorithm.Auto
        };
        context.Correspondents.Add(entity);
        await context.SaveChangesAsync();
        var entityId = entity.Id;

        // Act - hard delete is converted to soft delete by SoftDeleteInterceptor
        context.Correspondents.Remove(entity);
        await context.SaveChangesAsync();

        // Assert - entity should not appear in a LINQ query (global query filter excludes IsDeleted=true)
        var viaQuery = await context.Correspondents
            .FirstOrDefaultAsync(c => c.Id == entityId);
        viaQuery.Should().BeNull();

        // But should exist with IsDeleted=true when query filters are ignored
        var rawEntity = await context.Correspondents
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == entityId);
        rawEntity.Should().NotBeNull();
        rawEntity!.IsDeleted.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that multiple entities can be created with auto-generated IDs.
    /// </summary>
    [Fact]
    public async Task Create_Multiple_Entities()
    {
        // Arrange
        await using var context = Fixture.CreateContext();

        // Act
        var entityA = new Correspondent { Name = "Entity A", MatchingAlgorithm = Core.Documents.Enums.MatchingAlgorithm.Auto };
        var entityB = new Correspondent { Name = "Entity B", MatchingAlgorithm = Core.Documents.Enums.MatchingAlgorithm.Auto };
        var entityC = new Correspondent { Name = "Entity C", MatchingAlgorithm = Core.Documents.Enums.MatchingAlgorithm.Auto };
        context.Correspondents.AddRange(entityA, entityB, entityC);
        await context.SaveChangesAsync();

        // Assert - all entities got IDs assigned and are queryable
        entityA.Id.Should().BeGreaterThan(0);
        entityB.Id.Should().BeGreaterThan(0);
        entityC.Id.Should().BeGreaterThan(0);

        // Verify each by its own ID
        (await context.Correspondents.FirstOrDefaultAsync(c => c.Id == entityA.Id)).Should().NotBeNull();
        (await context.Correspondents.FirstOrDefaultAsync(c => c.Id == entityB.Id)).Should().NotBeNull();
        (await context.Correspondents.FirstOrDefaultAsync(c => c.Id == entityC.Id)).Should().NotBeNull();
    }
}
