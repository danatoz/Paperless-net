using System.Diagnostics.CodeAnalysis;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Microsoft.Extensions.Options;
using Directory = Lucene.Net.Store.Directory;

namespace Paperless.Infrastructure.Search;

/// <summary>
/// Manages the lifecycle of the Lucene.NET search index.
/// Provides thread-safe access to the index via <see cref="SearcherManager"/>
/// for concurrent reader/writer patterns.
/// </summary>
public sealed class SearchIndexManager : IDisposable
{
    private readonly Directory _directory;
    private readonly Analyzer _analyzer;
    private readonly IndexWriter _indexWriter;
    private readonly SearcherManager _searcherManager;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Gets the Lucene <see cref="Analyzer"/> used for text analysis.
    /// </summary>
    public Analyzer Analyzer => _analyzer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchIndexManager"/> class.
    /// Creates or opens the Lucene index at the configured directory path.
    /// </summary>
    /// <param name="options">Search index configuration options.</param>
    /// <exception cref="InvalidOperationException">Thrown when the index cannot be created or opened.</exception>
    public SearchIndexManager(IOptions<SearchIndexOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var indexDir = options.Value.IndexDirectory;
        if (string.IsNullOrWhiteSpace(indexDir))
            throw new ArgumentException("Index directory path must be specified.", nameof(options));

        // Ensure the directory exists
        var fullPath = System.IO.Path.GetFullPath(indexDir);
        System.IO.Directory.CreateDirectory(fullPath);

        // Open the Lucene directory
        _directory = FSDirectory.Open(fullPath);

        // Create the analyzer based on configuration
        _analyzer = CreateAnalyzer(options.Value.AnalyzerType);

        // Configure and create the IndexWriter
        var writerConfig = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, _analyzer)
        {
            RAMBufferSizeMB = options.Value.RamBufferSizeMb,
            UseCompoundFile = options.Value.UseCompoundFile,
            OpenMode = OpenMode.CREATE_OR_APPEND
        };

        _indexWriter = new IndexWriter(_directory, writerConfig);

        // Create the SearcherManager for concurrent reader/writer access
        _searcherManager = new SearcherManager(_indexWriter, applyAllDeletes: true, null);
    }

    /// <summary>
    /// Acquires a <see cref="IndexSearcher"/> from the <see cref="SearcherManager"/>
    /// for read operations. Must be returned via <see cref="ReleaseSearcher"/>.
    /// </summary>
    /// <returns>An active <see cref="IndexSearcher"/> for querying the index.</returns>
    public IndexSearcher AcquireSearcher()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _searcherManager.Acquire();
    }

    /// <summary>
    /// Releases a previously acquired <see cref="IndexSearcher"/> back to the manager pool.
    /// </summary>
    /// <param name="searcher">The searcher to release.</param>
    public void ReleaseSearcher([DisallowNull] IndexSearcher searcher)
    {
        ArgumentNullException.ThrowIfNull(searcher);
        _searcherManager.Release(searcher);
    }

    /// <summary>
    /// Gets the underlying <see cref="IndexWriter"/> for document add/update/delete operations.
    /// </summary>
    public IndexWriter Writer => _indexWriter;

    /// <summary>
    /// Commits all pending changes to the index.
    /// Should be called after batch operations.
    /// </summary>
    public void Commit()
    {
        _indexWriter.Commit();
    }

    /// <summary>
    /// Forces a merge of all index segments.
    /// This is an expensive operation and should be run periodically (e.g., daily).
    /// </summary>
    public void Optimize()
    {
        _indexWriter.ForceMerge(1);
        _indexWriter.Commit();
    }

    /// <summary>
    /// Deletes all documents from the index and resets it.
    /// </summary>
    public void Clear()
    {
        _indexWriter.DeleteAll();
        _indexWriter.Commit();
        RefreshSearchers();
    }

    /// <summary>
    /// Refreshes the <see cref="SearcherManager"/> so that subsequent
    /// <see cref="AcquireSearcher"/> calls see the latest index changes.
    /// </summary>
    public void RefreshSearchers()
    {
        _searcherManager.MaybeRefresh();
    }

    /// <summary>
    /// Creates a new <see cref="DirectoryReader"/> that sees the latest
    /// uncommitted changes (NRT — near-real-time reader).
    /// </summary>
    public DirectoryReader GetNrtReader()
    {
        return DirectoryReader.Open(_indexWriter, applyAllDeletes: true);
    }

    private static Analyzer CreateAnalyzer(string analyzerType)
    {
        var version = Lucene.Net.Util.LuceneVersion.LUCENE_48;
        return analyzerType?.ToLowerInvariant() switch
        {
            "simple" => new SimpleAnalyzer(version),
            "whitespace" => new WhitespaceAnalyzer(version),
            "keyword" => new KeywordAnalyzer(),
            _ => new StandardAnalyzer(version)
        };
    }

    /// <summary>
    /// Disposes the search index manager, releasing all resources.
    /// Commits pending changes before closing.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            if (_disposed)
                return;

            _disposed = true;
            _searcherManager?.Dispose();
            _indexWriter?.Dispose();
            _directory?.Dispose();
            _analyzer?.Dispose();
        }
    }
}
