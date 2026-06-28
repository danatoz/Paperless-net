using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Paperless.Core.Common.Specifications;
using Paperless.Core.Documents.Entities;
using Paperless.Infrastructure.Persistence.Repositories;

namespace Paperless.Infrastructure.Tests.Persistence;

public class DocumentRepositoryTests : RepositoryTestsBase
{
    [Fact]
    public async Task AddAsync_Should_Persist_Document()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new DocumentRepository(context, CreateUnitOfWorkMock());

        var document = new Document
        {
            Title = "Test Document",
            Content = "Test content",
            Checksum = "abc123"
        };

        // Act
        var result = await repo.AddAsync(document);
        await context.SaveChangesAsync();

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("Test Document");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_For_NonExistent()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new DocumentRepository(context, CreateUnitOfWorkMock());

        // Act
        var result = await repo.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Document_With_Inclusions()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new DocumentRepository(context, CreateUnitOfWorkMock());

        var tag = new Tag { Name = "TestTag", MatchingAlgorithm = Core.Documents.Enums.MatchingAlgorithm.Auto };
        context.Tags.Add(tag);
        await context.SaveChangesAsync();

        var document = new Document
        {
            Title = "Doc with Tags",
            Content = "Has tags",
            Checksum = "checksum456",
            Tags = { tag }
        };
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByIdAsync(document.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Doc with Tags");
        result.Tags.Should().HaveCount(1);
        result.Tags.First().Name.Should().Be("TestTag");
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_All_Documents()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new DocumentRepository(context, CreateUnitOfWorkMock());

        context.Documents.AddRange(
            new Document { Title = "Doc1", Checksum = "c1" },
            new Document { Title = "Doc2", Checksum = "c2" },
            new Document { Title = "Doc3", Checksum = "c3" }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_With_Spec_Should_Support_Pagination()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new DocumentRepository(context, CreateUnitOfWorkMock());

        for (int i = 1; i <= 10; i++)
        {
            context.Documents.Add(new Document { Title = $"Doc{i}", Checksum = $"c{i}" });
        }
        await context.SaveChangesAsync();

        var spec = new Specification<Document>()
            .OrderByAscending(d => d.Title)
            .ApplyPaging(1, 3);

        // Act
        var result = await repo.GetAllAsync(spec);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(3);
        result.TotalPages.Should().Be(4);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetByChecksumAsync_Should_Find_By_Checksum()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new DocumentRepository(context, CreateUnitOfWorkMock());

        context.Documents.Add(new Document { Title = "Unique", Checksum = "unique-checksum-789" });
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByChecksumAsync("unique-checksum-789");

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Unique");
    }

    [Fact]
    public async Task Delete_Should_SoftDelete()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new DocumentRepository(context, CreateUnitOfWorkMock());

        var document = new Document { Title = "ToDelete", Checksum = "del" };
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        // Act
        repo.Delete(document);
        await context.SaveChangesAsync();

        // Assert - Document should not appear in default query (filtered by IsDeleted)
        var deleted = await repo.GetByIdAsync(document.Id);
        deleted.Should().BeNull();

        // But should still exist in DB with IsDeleted=true
        var rawDoc = await context.Documents
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == document.Id);
        rawDoc.Should().NotBeNull();
        rawDoc!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_Should_Find_By_Title()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new DocumentRepository(context, CreateUnitOfWorkMock());

        context.Documents.AddRange(
            new Document { Title = "Invoice March", Content = "March invoice content", Checksum = "im" },
            new Document { Title = "Receipt April", Content = "April receipt content", Checksum = "ra" }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await repo.SearchAsync("Invoice");

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Invoice March");
    }

    [Fact]
    public async Task SearchAsync_Should_Find_By_Content()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new DocumentRepository(context, CreateUnitOfWorkMock());

        context.Documents.AddRange(
            new Document { Title = "Doc1", Content = "Important memo about taxes", Checksum = "d1" },
            new Document { Title = "Doc2", Content = "Shopping list", Checksum = "d2" }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await repo.SearchAsync("taxes");

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Doc1");
    }

    [Fact]
    public async Task GetByCorrespondentAsync_Should_Filter()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new DocumentRepository(context, CreateUnitOfWorkMock());

        var corr = new Correspondent { Name = "ABC Corp", MatchingAlgorithm = Core.Documents.Enums.MatchingAlgorithm.Auto };
        context.Correspondents.Add(corr);
        await context.SaveChangesAsync();

        context.Documents.Add(new Document { Title = "ABC Invoice", CorrespondentId = corr.Id, Checksum = "abc" });
        context.Documents.Add(new Document { Title = "Other Doc", Checksum = "other" });
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByCorrespondentAsync(corr.Id);

        // Assert
        result.Should().HaveCount(1);
        result.First().CorrespondentId.Should().Be(corr.Id);
    }

    [Fact]
    public async Task GetByTagsAsync_Should_Filter_By_Tags()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new DocumentRepository(context, CreateUnitOfWorkMock());

        var urgentTag = new Tag { Name = "Urgent", MatchingAlgorithm = Core.Documents.Enums.MatchingAlgorithm.Auto };
        var financeTag = new Tag { Name = "Finance", MatchingAlgorithm = Core.Documents.Enums.MatchingAlgorithm.Auto };
        context.Tags.AddRange(urgentTag, financeTag);
        await context.SaveChangesAsync();

        context.Documents.Add(new Document { Title = "Urgent Doc", Tags = { urgentTag }, Checksum = "u1" });
        context.Documents.Add(new Document { Title = "Finance Doc", Tags = { financeTag }, Checksum = "f1" });
        context.Documents.Add(new Document { Title = "Both Doc", Tags = { urgentTag, financeTag }, Checksum = "b1" });
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByTagsAsync(new[] { urgentTag.Id });

        // Assert
        result.Should().HaveCount(2); // Urgent Doc + Both Doc
    }

    [Fact]
    public async Task BulkDeleteAsync_Should_SoftDelete_Matching_Docs()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new DocumentRepository(context, CreateUnitOfWorkMock());

        for (int i = 1; i <= 5; i++)
        {
            context.Documents.Add(new Document { Title = $"Doc{i}", Checksum = $"c{i}" });
        }
        await context.SaveChangesAsync();

        var spec = new Specification<Document>(d => d.Title!.StartsWith("Doc") && d.Id > 2);

        // Act
        var count = await repo.BulkDeleteAsync(spec);
        await context.SaveChangesAsync();

        // Assert
        count.Should().Be(3); // Docs 3, 4, 5

        var remaining = await repo.GetAllAsync();
        remaining.Should().HaveCount(2); // Docs 1, 2 (not deleted)
    }

    [Fact]
    public async Task Update_Should_Modify_Document()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new DocumentRepository(context, CreateUnitOfWorkMock());

        var document = new Document { Title = "Original", Content = "Original content", Checksum = "chk" };
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        // Act
        document.Title = "Updated Title";
        repo.Update(document);
        await context.SaveChangesAsync();

        // Assert
        var updated = await repo.GetByIdAsync(document.Id);
        updated!.Title.Should().Be("Updated Title");
    }
}
