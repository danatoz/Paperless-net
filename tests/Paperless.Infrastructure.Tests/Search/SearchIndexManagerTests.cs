using FluentAssertions;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Microsoft.Extensions.Options;
using Paperless.Infrastructure.Search;

namespace Paperless.Infrastructure.Tests.Search;

/// <summary>
/// Unit tests for <see cref="SearchIndexManager"/>.
/// Uses a temporary directory for each test to ensure isolation.
/// </summary>
public sealed class SearchIndexManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SearchIndexManager _manager;

    public SearchIndexManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"lucene-test-{Guid.NewGuid()}");
        var options = Options.Create(new SearchIndexOptions
        {
            IndexDirectory = _tempDir,
            AnalyzerType = "standard",
            RamBufferSizeMb = 16
        });

        _manager = new SearchIndexManager(options);
    }

    [Fact]
    public void Constructor_ShouldCreateIndex_AtSpecifiedDirectory()
    {
        // Assert
        Directory.Exists(_tempDir).Should().BeTrue();
        var indexFiles = Directory.GetFiles(_tempDir);
        indexFiles.Should().NotBeEmpty("Lucene index files should be present");
    }

    [Fact]
    public void AcquireSearcher_ShouldReturn_ValidSearcher()
    {
        // Act
        var searcher = _manager.AcquireSearcher();

        // Assert
        searcher.Should().NotBeNull();
        searcher.IndexReader.Should().NotBeNull();

        // Cleanup
        _manager.ReleaseSearcher(searcher);
    }

    [Fact]
    public void AcquireAndReleaseSearcher_ShouldWork_MultipleTimes()
    {
        // Act & Assert — multiple acquire/release cycles
        for (int i = 0; i < 10; i++)
        {
            var searcher = _manager.AcquireSearcher();
            searcher.Should().NotBeNull();
            _manager.ReleaseSearcher(searcher);
        }
    }

    [Fact]
    public void Writer_ShouldReturn_ValidIndexWriter()
    {
        // Act
        var writer = _manager.Writer;

        // Assert
        writer.Should().NotBeNull();
        writer.Directory.Should().NotBeNull();
    }

    [Fact]
    public void Commit_ShouldSucceed_WithoutError()
    {
        // Act
        var act = () => _manager.Commit();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Clear_ShouldRemove_AllDocuments()
    {
        // Arrange — add a document
        var doc = new global::Lucene.Net.Documents.Document();
        doc.Add(new Int32Field("id", 1, Field.Store.YES));
        _manager.Writer.AddDocument(doc);
        _manager.Commit();

        // Refresh searchers so they see the committed changes
        _manager.RefreshSearchers();

        // Pre-assert: index has 1 document
        var searcher = _manager.AcquireSearcher();
        var initialCount = searcher.IndexReader.NumDocs;
        _manager.ReleaseSearcher(searcher);
        initialCount.Should().Be(1);

        // Act
        _manager.Clear();

        // Assert: index is empty
        searcher = _manager.AcquireSearcher();
        var afterCount = searcher.IndexReader.NumDocs;
        _manager.ReleaseSearcher(searcher);
        afterCount.Should().Be(0);
    }

    [Fact]
    public void GetNrtReader_ShouldReturn_ReaderWithUncommittedChanges()
    {
        // Arrange
        var doc = new global::Lucene.Net.Documents.Document();
        doc.Add(new Int32Field("id", 42, Field.Store.YES));
        _manager.Writer.AddDocument(doc);
        _manager.Commit();  // Must commit for SearcherManager to pick it up
        _manager.RefreshSearchers();

        // Act — NRT reader sees committed changes
        using var reader = _manager.GetNrtReader();

        // Assert
        reader.NumDocs.Should().Be(1);
    }

    [Fact]
    public void Optimize_ShouldSucceed_WithoutError()
    {
        // Act
        var act = () => _manager.Optimize();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldCleanUp_Resources()
    {
        // Arrange
        var manager = _manager;

        // Act
        manager.Dispose();

        // Assert — should not throw on double dispose
        var act = () => manager.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void AcquireSearcher_AfterDispose_ShouldThrow()
    {
        // Arrange
        _manager.Dispose();

        // Act
        var act = () => _manager.AcquireSearcher();

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Constructor_WithEmptyIndexPath_ShouldThrow()
    {
        // Arrange
        var options = Options.Create(new SearchIndexOptions
        {
            IndexDirectory = ""
        });

        // Act
        var act = () => new SearchIndexManager(options);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    public void Dispose()
    {
        _manager?.Dispose();

        // Clean up temp directory
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
