using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Paperless.Core.Documents.Entities;
using Paperless.Infrastructure.Search;

namespace Paperless.Infrastructure.Tests.Search;

/// <summary>
/// Integration tests for <see cref="LuceneSearchBackend"/>.
/// Uses a temporary Lucene index directory for each test class.
/// Tests cover indexing, search, deletion, autocomplete, and the richer SearchDocumentsAsync API.
/// </summary>
public sealed class LuceneSearchBackendTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SearchIndexManager _indexManager;
    private readonly LuceneSearchBackend _backend;

    public LuceneSearchBackendTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"lucene-test-{Guid.NewGuid()}");

        var options = Options.Create(new SearchIndexOptions
        {
            IndexDirectory = _tempDir,
            AnalyzerType = "standard",
            RamBufferSizeMb = 16
        });

        _indexManager = new SearchIndexManager(options);
        _backend = new LuceneSearchBackend(
            _indexManager,
            new NullLogger<LuceneSearchBackend>());
    }

    // ── Helper methods ───────────────────────────────────────────

    private Document CreateTestDocument(
        int id,
        string title,
        string content,
        int? correspondentId = null,
        string? correspondentName = null,
        int? documentTypeId = null,
        string? documentTypeName = null,
        IEnumerable<Tag>? tags = null,
        DateTime? created = null,
        DateTime? added = null,
        int? ownerId = null,
        string? storagePath = null,
        int? archiveSerialNumber = null)
    {
        var doc = new Document
        {
            Id = id,
            Title = title,
            Content = content,
            CorrespondentId = correspondentId,
            DocumentTypeId = documentTypeId,
            Created = created ?? DateTime.UtcNow,
            Added = added ?? DateTime.UtcNow,
            OwnerId = ownerId,
            StoragePath = storagePath,
            ArchiveSerialNumber = archiveSerialNumber
        };

        if (correspondentName != null)
        {
            doc.Correspondent = new Correspondent
            {
                Id = correspondentId ?? 0,
                Name = correspondentName
            };
        }

        if (documentTypeName != null)
        {
            doc.DocumentType = new DocumentType
            {
                Id = documentTypeId ?? 0,
                Name = documentTypeName
            };
        }

        if (tags != null)
        {
            foreach (var tag in tags)
            {
                doc.Tags.Add(tag);
            }
        }

        return doc;
    }

    private Tag CreateTag(int id, string name)
    {
        return new Tag { Id = id, Name = name };
    }

    private async Task IndexTestDocuments()
    {
        var docs = new[]
        {
            CreateTestDocument(
                1, "Invoice March 2024", "Invoice for office supplies purchased in March",
                correspondentId: 1, correspondentName: "OfficeMax",
                documentTypeId: 1, documentTypeName: "Invoice",
                tags: new[] { CreateTag(1, "important"), CreateTag(2, "finance") },
                created: new DateTime(2024, 3, 15), added: new DateTime(2024, 3, 16)),

            CreateTestDocument(
                2, "Receipt Coffee Machine", "Coffee machine receipt and warranty information",
                correspondentId: 2, correspondentName: "Starbucks Corp",
                documentTypeId: 2, documentTypeName: "Receipt",
                tags: new[] { CreateTag(2, "finance") },
                created: new DateTime(2024, 4, 10), added: new DateTime(2024, 4, 11)),

            CreateTestDocument(
                3, "Contract Renewal", "Annual contract renewal for office lease",
                correspondentId: 1, correspondentName: "OfficeMax",
                documentTypeId: 3, documentTypeName: "Contract",
                tags: new[] { CreateTag(3, "legal"), CreateTag(1, "important") },
                created: new DateTime(2024, 1, 1), added: new DateTime(2024, 1, 5)),

            CreateTestDocument(
                4, "Personal Note", "Just a personal note about the team meeting",
                tags: new[] { CreateTag(4, "personal") },
                created: new DateTime(2024, 5, 1), added: new DateTime(2024, 5, 2))
        };

        foreach (var doc in docs)
        {
            await _backend.IndexDocumentAsync(doc);
        }
    }

    // ── IndexDocumentAsync tests ─────────────────────────────────

    [Fact]
    public async Task IndexDocumentAsync_ShouldIndex_NewDocument()
    {
        // Arrange
        var doc = CreateTestDocument(100, "Test Document", "Test content for indexing");

        // Act
        await _backend.IndexDocumentAsync(doc);

        // Assert
        var ids = await _backend.SearchAsync("Test content");
        ids.Should().Contain(100);
    }

    [Fact]
    public async Task IndexDocumentAsync_ShouldUpdate_ExistingDocument()
    {
        // Arrange
        var doc = CreateTestDocument(200, "Original Title", "Original content");
        await _backend.IndexDocumentAsync(doc);

        // Update
        doc.Title = "Updated Title";
        doc.Content = "Updated content";
        await _backend.IndexDocumentAsync(doc);

        // Assert — old content should not be found, new content should
        var oldResults = await _backend.SearchAsync("Original");
        oldResults.Should().BeEmpty("old content should be replaced");

        var newResults = await _backend.SearchAsync("Updated");
        newResults.Should().Contain(200);
    }

    // ── RemoveFromIndexAsync tests ───────────────────────────────

    [Fact]
    public async Task RemoveFromIndexAsync_ShouldRemove_Document()
    {
        // Arrange
        var doc = CreateTestDocument(300, "To Delete", "This document will be deleted");
        await _backend.IndexDocumentAsync(doc);

        // Pre-assert
        var before = await _backend.SearchAsync("delete");
        before.Should().Contain(300);

        // Act
        await _backend.RemoveFromIndexAsync(300);

        // Assert
        var after = await _backend.SearchAsync("delete");
        after.Should().NotContain(300);
    }

    [Fact]
    public async Task RemoveFromIndexAsync_ShouldNotThrow_ForNonExistentDocument()
    {
        // Act
        var act = () => _backend.RemoveFromIndexAsync(99999);

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ── SearchAsync (basic) tests ────────────────────────────────

    [Fact]
    public async Task SearchAsync_ShouldReturnMatching_DocumentIds()
    {
        // Arrange
        await IndexTestDocuments();

        // Act
        var results = await _backend.SearchAsync("invoice");

        // Assert
        results.Should().Contain(1);
        results.Should().NotContain(4); // "Personal Note" has no invoice-related text
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmpty_ForNonMatchingQuery()
    {
        // Arrange
        await IndexTestDocuments();

        // Act
        var results = await _backend.SearchAsync("xyznonexistent12345");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmpty_ForEmptyQuery()
    {
        // Arrange
        await IndexTestDocuments();

        // Act
        var results = await _backend.SearchAsync("");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        await IndexTestDocuments();

        // Act
        var lowerResults = await _backend.SearchAsync("invoice");
        var upperResults = await _backend.SearchAsync("INVOICE");
        var mixedResults = await _backend.SearchAsync("InVoIcE");

        // Assert
        lowerResults.Should().BeEquivalentTo(upperResults);
        lowerResults.Should().BeEquivalentTo(mixedResults);
    }

    // ── AutocompleteAsync tests ──────────────────────────────────

    [Fact]
    public async Task AutocompleteAsync_ShouldReturnSuggestions()
    {
        // Arrange
        await IndexTestDocuments();

        // Act
        var suggestions = await _backend.AutocompleteAsync("invo");

        // Assert
        suggestions.Should().NotBeEmpty();
        suggestions.Should().ContainMatch("*invo*");
    }

    [Fact]
    public async Task AutocompleteAsync_ShouldReturnEmpty_ForEmptyPrefix()
    {
        // Arrange
        await IndexTestDocuments();

        // Act
        var suggestions = await _backend.AutocompleteAsync("");

        // Assert
        suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task AutocompleteAsync_ShouldReturnEmpty_ForNonMatchingPrefix()
    {
        // Arrange
        await IndexTestDocuments();

        // Act
        var suggestions = await _backend.AutocompleteAsync("xyzzzzz");

        // Assert
        suggestions.Should().BeEmpty();
    }

    // ── SearchDocumentsAsync (rich search) tests ─────────────────

    [Fact]
    public async Task SearchDocumentsAsync_ShouldReturnPagedResults()
    {
        // Arrange
        await IndexTestDocuments();

        var query = new SearchQuery
        {
            Query = "office",
            Page = 1,
            PageSize = 1
        };

        // Act
        var result = await _backend.SearchDocumentsAsync(query);

        // Assert
        result.DocumentIds.Should().HaveCount(1);
        result.TotalCount.Should().Be(2); // 2 docs contain "office"
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(1);
        result.TotalPages.Should().Be(2);
        result.HasNext.Should().BeTrue();
        result.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task SearchDocumentsAsync_ShouldApply_TagFilter()
    {
        // Arrange
        await IndexTestDocuments();

        var query = new SearchQuery
        {
            Query = "",
            TagIds = new[] { 1 } // "important" tag
        };

        // Act
        var result = await _backend.SearchDocumentsAsync(query);

        // Assert — only docs with tag id=1 (important)
        result.DocumentIds.Should().Contain(1); // Invoice March 2024 (important)
        result.DocumentIds.Should().Contain(3); // Contract Renewal (important)
        result.DocumentIds.Should().NotContain(2); // Receipt (finance only)
        result.DocumentIds.Should().NotContain(4); // Personal Note (personal only)
    }

    [Fact]
    public async Task SearchDocumentsAsync_ShouldApply_CorrespondentFilter()
    {
        // Arrange
        await IndexTestDocuments();

        var query = new SearchQuery
        {
            Query = "",
            CorrespondentId = 1 // OfficeMax
        };

        // Act
        var result = await _backend.SearchDocumentsAsync(query);

        // Assert
        result.DocumentIds.Should().Contain(1); // Invoice March 2024 (OfficeMax)
        result.DocumentIds.Should().Contain(3); // Contract Renewal (OfficeMax)
        result.DocumentIds.Should().NotContain(2); // Receipt (Starbucks)
        result.DocumentIds.Should().NotContain(4); // Personal Note (no correspondent)
    }

    [Fact]
    public async Task SearchDocumentsAsync_ShouldApply_DocumentTypeFilter()
    {
        // Arrange
        await IndexTestDocuments();

        var query = new SearchQuery
        {
            Query = "",
            DocumentTypeId = 1 // Invoice
        };

        // Act
        var result = await _backend.SearchDocumentsAsync(query);

        // Assert
        result.DocumentIds.Should().Contain(1); // Invoice March 2024
        result.DocumentIds.Should().NotContain(2);
        result.DocumentIds.Should().NotContain(3);
        result.DocumentIds.Should().NotContain(4);
    }

    [Fact]
    public async Task SearchDocumentsAsync_ShouldApply_DateRangeFilter()
    {
        // Arrange
        await IndexTestDocuments();

        var query = new SearchQuery
        {
            Query = "",
            CreatedAfter = new DateTime(2024, 3, 1),
            CreatedBefore = new DateTime(2024, 4, 30)
        };

        // Act
        var result = await _backend.SearchDocumentsAsync(query);

        // Assert — only docs created in March-April 2024
        result.DocumentIds.Should().Contain(1); // 2024-03-15
        result.DocumentIds.Should().Contain(2); // 2024-04-10
        result.DocumentIds.Should().NotContain(3); // 2024-01-01
        result.DocumentIds.Should().NotContain(4); // 2024-05-01
    }

    [Fact]
    public async Task SearchDocumentsAsync_ShouldCombine_FullTextAndFilters()
    {
        // Arrange
        await IndexTestDocuments();

        var query = new SearchQuery
        {
            Query = "contract",
            CorrespondentId = 1 // OfficeMax
        };

        // Act
        var result = await _backend.SearchDocumentsAsync(query);

        // Assert — only Contract Renewal (OfficeMax) contains "contract"
        result.DocumentIds.Should().Contain(3);
        result.DocumentIds.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchDocumentsAsync_ShouldThrow_OnInvalidPagination()
    {
        // Act
        var act = () => _backend.SearchDocumentsAsync(new SearchQuery
        {
            Query = "test",
            Page = 0 // Invalid
        });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SearchDocumentsAsync_ShouldReturnAll_WhenEmptyQuery()
    {
        // Arrange
        await IndexTestDocuments();

        var query = new SearchQuery
        {
            Query = "",
            PageSize = 100
        };

        // Act
        var result = await _backend.SearchDocumentsAsync(query);

        // Assert — should return all 4 documents
        result.DocumentIds.Should().HaveCount(4);
        result.TotalCount.Should().Be(4);
    }

    // ── CreateLuceneDocument tests ───────────────────────────────

    [Fact]
    public void CreateLuceneDocument_ShouldMap_AllFields()
    {
        // Arrange
        var created = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var added = new DateTime(2024, 6, 16, 8, 0, 0, DateTimeKind.Utc);

        var doc = CreateTestDocument(
            id: 42,
            title: "Test Title",
            content: "Test Content",
            correspondentId: 5,
            correspondentName: "Test Corp",
            documentTypeId: 3,
            documentTypeName: "Report",
            tags: new[] { CreateTag(1, "tag1"), CreateTag(2, "tag2") },
            created: created,
            added: added,
            ownerId: 100,
            storagePath: "test/path",
            archiveSerialNumber: 999);

        // Act
        var luceneDoc = _backend.CreateLuceneDocument(doc);

        // Assert — stored fields should be retrievable
        luceneDoc.GetField(LuceneSearchBackend.FieldNames.Id).Should().NotBeNull();
        luceneDoc.Get(LuceneSearchBackend.FieldNames.Id).Should().Be("42");

        luceneDoc.GetField(LuceneSearchBackend.FieldNames.Title).Should().NotBeNull();
        luceneDoc.Get(LuceneSearchBackend.FieldNames.Title).Should().Be("Test Title");

        luceneDoc.GetField(LuceneSearchBackend.FieldNames.Content).Should().NotBeNull();
        luceneDoc.Get(LuceneSearchBackend.FieldNames.Content).Should().Be("Test Content");

        luceneDoc.GetField(LuceneSearchBackend.FieldNames.Correspondent).Should().NotBeNull();
        luceneDoc.Get(LuceneSearchBackend.FieldNames.Correspondent).Should().Be("Test Corp");

        luceneDoc.GetField(LuceneSearchBackend.FieldNames.DocumentType).Should().NotBeNull();
        luceneDoc.Get(LuceneSearchBackend.FieldNames.DocumentType).Should().Be("Report");

        luceneDoc.GetField(LuceneSearchBackend.FieldNames.StoragePath).Should().NotBeNull();
        luceneDoc.Get(LuceneSearchBackend.FieldNames.StoragePath).Should().Be("test/path");
    }

    [Fact]
    public void CreateLuceneDocument_ShouldMap_TagsAsMultiValued()
    {
        // Arrange
        var doc = CreateTestDocument(
            55, "Tags Test", "Testing tags",
            tags: new[] { CreateTag(10, "tag-alpha"), CreateTag(20, "tag-beta") });

        // Act
        var luceneDoc = _backend.CreateLuceneDocument(doc);

        // Assert — tag_ids should have multiple values
        var tagIdsFields = luceneDoc.GetFields(LuceneSearchBackend.FieldNames.TagIds);
        tagIdsFields.Should().HaveCount(2);

        var tagsFields = luceneDoc.GetFields(LuceneSearchBackend.FieldNames.Tags);
        tagsFields.Should().HaveCount(2);
    }

    [Fact]
    public void ToEpochMilliseconds_ShouldRoundTrip_Correctly()
    {
        // Arrange
        var original = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var millis = LuceneSearchBackend.ToEpochMilliseconds(original);
        var restored = LuceneSearchBackend.FromEpochMilliseconds(millis);

        // Assert
        restored.Should().Be(original);
    }

    // ── OptimizeIndex / ClearIndex tests ─────────────────────────

    [Fact]
    public async Task ClearIndex_ShouldRemove_AllDocuments()
    {
        // Arrange
        var doc = CreateTestDocument(77, "Clear Test", "Will be cleared");
        await _backend.IndexDocumentAsync(doc);

        // Pre-assert
        var before = await _backend.SearchAsync("cleared");
        before.Should().NotBeEmpty();

        // Act
        _backend.ClearIndex();

        // Assert
        var after = await _backend.SearchAsync("cleared");
        after.Should().BeEmpty();
    }

    [Fact]
    public void OptimizeIndex_ShouldNotThrow()
    {
        // Act
        var act = () => _backend.OptimizeIndex();

        // Assert
        act.Should().NotThrow();
    }

    public void Dispose()
    {
        _backend?.Dispose();

        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup failures
            }
        }
    }
}
