using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Paperless.Core.Documents.Entities;
using Paperless.Core.Documents.Enums;
using Paperless.Infrastructure.Persistence.Repositories;

namespace Paperless.Infrastructure.Tests.Persistence;

public class MatchingModelRepositoriesTests : RepositoryTestsBase
{
    [Fact]
    public async Task CorrespondentRepository_AddAndGet()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new CorrespondentRepository(context, CreateUnitOfWorkMock());

        var entity = new Correspondent
        {
            Name = "Test Correspondent",
            MatchingAlgorithm = MatchingAlgorithm.Auto
        };

        // Act
        await repo.AddAsync(entity);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await repo.GetByIdAsync(entity.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Test Correspondent");
    }

    [Fact]
    public async Task CorrespondentRepository_Delete_SoftDelete()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new CorrespondentRepository(context, CreateUnitOfWorkMock());

        var entity = new Correspondent { Name = "ToDelete", MatchingAlgorithm = MatchingAlgorithm.Auto };
        await repo.AddAsync(entity);
        await context.SaveChangesAsync();

        // Act
        repo.Delete(entity);
        await context.SaveChangesAsync();

        // Assert: soft-delete sets IsDeleted flag
        // Note: only Document has a global query filter; for other entities
        // the soft-delete is applied at the object level (IsDeleted = true)
        // and must be respected by application code.
        var deleted = await repo.GetByIdAsync(entity.Id);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task TagRepository_AddAndGet()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new TagRepository(context, CreateUnitOfWorkMock());

        var entity = new Tag
        {
            Name = "Important",
            Color = "#FF0000",
            TextColor = "#FFFFFF",
            MatchingAlgorithm = MatchingAlgorithm.Auto
        };

        // Act
        await repo.AddAsync(entity);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await repo.GetByIdAsync(entity.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Important");
        retrieved.Color.Should().Be("#FF0000");
    }

    [Fact]
    public async Task DocumentTypeRepository_AddAndGet()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new DocumentTypeRepository(context, CreateUnitOfWorkMock());

        var entity = new DocumentType
        {
            Name = "Invoice",
            MatchingAlgorithm = MatchingAlgorithm.Auto
        };

        // Act
        await repo.AddAsync(entity);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await repo.GetByIdAsync(entity.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Invoice");
    }

    [Fact]
    public async Task StoragePathRepository_AddAndGet()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new StoragePathRepository(context, CreateUnitOfWorkMock());

        var entity = new StoragePath
        {
            Name = "Default Path",
            PathTemplate = "{correspondent}/{title}",
            MatchingAlgorithm = MatchingAlgorithm.Auto
        };

        // Act
        await repo.AddAsync(entity);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await repo.GetByIdAsync(entity.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Default Path");
        retrieved.PathTemplate.Should().Be("{correspondent}/{title}");
    }

    [Fact]
    public async Task CustomFieldRepository_AddAndGet()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new CustomFieldRepository(context, CreateUnitOfWorkMock());

        var entity = new CustomField
        {
            Name = "Invoice Number",
            Type = CustomFieldType.Text
        };

        // Act
        await repo.AddAsync(entity);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await repo.GetByIdAsync(entity.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Invoice Number");
        retrieved.Type.Should().Be(CustomFieldType.Text);
    }
}
