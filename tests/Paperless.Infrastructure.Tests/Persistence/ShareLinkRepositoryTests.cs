using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Paperless.Core.Documents.Entities;
using Paperless.Infrastructure.Persistence.Repositories;

namespace Paperless.Infrastructure.Tests.Persistence;

public class ShareLinkRepositoryTests : RepositoryTestsBase
{
    [Fact]
    public async Task AddAndGetShareLink()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new ShareLinkRepository(context, CreateUnitOfWorkMock());

        var doc = new Document { Title = "Shared Doc", Checksum = "share1" };
        context.Documents.Add(doc);
        await context.SaveChangesAsync();

        var link = new ShareLink
        {
            DocumentId = doc.Id,
            Slug = "my-share-link",
            FileVersion = ShareLink.FileVersionType.Archive
        };

        // Act
        await repo.AddAsync(link);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await repo.GetByIdAsync(link.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Slug.Should().Be("my-share-link");
        retrieved.DocumentId.Should().Be(doc.Id);
    }

    [Fact]
    public async Task GetBySlugAsync_Finds_Link()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new ShareLinkRepository(context, CreateUnitOfWorkMock());

        var doc = new Document { Title = "Slug Doc", Checksum = "slug1" };
        context.Documents.Add(doc);
        await context.SaveChangesAsync();

        var link = new ShareLink
        {
            DocumentId = doc.Id,
            Slug = "unique-slug-123",
            FileVersion = ShareLink.FileVersionType.Origin
        };
        context.ShareLinks.Add(link);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetBySlugAsync("unique-slug-123");

        // Assert
        result.Should().NotBeNull();
        result!.Slug.Should().Be("unique-slug-123");
    }

    [Fact]
    public async Task GetByDocumentIdAsync_Returns_Links_For_Document()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new ShareLinkRepository(context, CreateUnitOfWorkMock());

        var doc1 = new Document { Title = "Doc1", Checksum = "d1s" };
        var doc2 = new Document { Title = "Doc2", Checksum = "d2s" };
        context.Documents.AddRange(doc1, doc2);
        await context.SaveChangesAsync();

        context.ShareLinks.AddRange(
            new ShareLink { DocumentId = doc1.Id, Slug = "link1", FileVersion = ShareLink.FileVersionType.Archive },
            new ShareLink { DocumentId = doc1.Id, Slug = "link2", FileVersion = ShareLink.FileVersionType.Archive },
            new ShareLink { DocumentId = doc2.Id, Slug = "link3", FileVersion = ShareLink.FileVersionType.Origin }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByDocumentIdAsync(doc1.Id);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Bundle_Operations_Work()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new ShareLinkRepository(context, CreateUnitOfWorkMock());

        var bundle = new ShareLinkBundle
        {
            Slug = "bundle-slug"
        };

        // Act - Add bundle
        await repo.AddBundleAsync(bundle);
        await context.SaveChangesAsync();

        // Assert
        bundle.Id.Should().BeGreaterThan(0);

        // Act - Get by slug
        var retrieved = await repo.GetBundleBySlugAsync("bundle-slug");
        retrieved.Should().NotBeNull();
        retrieved!.Slug.Should().Be("bundle-slug");
    }

    [Fact]
    public async Task Delete_ShareLink_SoftDelete()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new ShareLinkRepository(context, CreateUnitOfWorkMock());

        var doc = new Document { Title = "Delete Test", Checksum = "del-share" };
        context.Documents.Add(doc);
        await context.SaveChangesAsync();

        var link = new ShareLink
        {
            DocumentId = doc.Id,
            Slug = "to-delete",
            FileVersion = ShareLink.FileVersionType.Archive
        };
        context.ShareLinks.Add(link);
        await context.SaveChangesAsync();

        // Act
        repo.Delete(link);
        await context.SaveChangesAsync();

        // Assert
        // Use IgnoreQueryFilters to verify the soft-delete record still exists in DB
        var deleted = await context.ShareLinks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(sl => sl.Id == link.Id);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }
}
