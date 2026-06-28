using System.Diagnostics.CodeAnalysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;
using Paperless.Core.Common.Interfaces;
using Paperless.Core.Documents.Entities;
using Document = Paperless.Core.Documents.Entities.Document;

namespace Paperless.Infrastructure.Search;

/// <summary>
/// Full-text search backend implementation using Lucene.NET.
/// Implements <see cref="ISearchBackend"/> for basic search operations
/// and provides additional methods for advanced search with filtering and pagination.
/// 
/// Field mapping (matching the original Tantivy schema):
/// - <c>id</c> — Int32Field (stored, indexed)
/// - <c>title</c> — TextField (stored, indexed, analyzed)
/// - <c>content</c> — TextField (stored, indexed, analyzed)
/// - <c>correspondent</c> — StringField (stored, indexed)
/// - <c>correspondent_id</c> — Int32Field (indexed)
/// - <c>document_type</c> — StringField (stored, indexed)
/// - <c>document_type_id</c> — Int32Field (indexed)
/// - <c>tag_ids</c> — Int32Field (indexed, multi-valued)
/// - <c>tags</c> — StringField (indexed, multi-valued, stored as space-separated)
/// - <c>created</c> — Int64Field (stored, indexed; milliseconds since epoch)
/// - <c>added</c> — Int64Field (stored, indexed; milliseconds since epoch)
/// - <c>is_inbox</c> — Int32Field (indexed; 0 or 1)
/// - <c>owner_id</c> — Int32Field (indexed)
/// - <c>storage_path</c> — StringField (stored, indexed)
/// - <c>archive_serial_number</c> — Int32Field (indexed)
/// </summary>
public sealed class LuceneSearchBackend : ISearchBackend, IDisposable
{
    private readonly SearchIndexManager _indexManager;
    private readonly ILogger<LuceneSearchBackend> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LuceneSearchBackend"/> class.
    /// </summary>
    /// <param name="indexManager">The search index lifecycle manager.</param>
    /// <param name="logger">Logger instance.</param>
    public LuceneSearchBackend(
        SearchIndexManager indexManager,
        ILogger<LuceneSearchBackend> logger)
    {
        _indexManager = indexManager ?? throw new ArgumentNullException(nameof(indexManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ────────────────────────────────────────────────────────────────
    //  ISearchBackend implementation
    // ────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task IndexDocumentAsync(Document document, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        ct.ThrowIfCancellationRequested();

        var luceneDoc = CreateLuceneDocument(document);
        // Delete existing document by numeric ID range, then add new one.
        // Term-based UpdateDocument doesn't work with Int32Field.
        _indexManager.Writer.DeleteDocuments(
            NumericRangeQuery.NewInt32Range(
                FieldNames.Id, document.Id, document.Id, true, true));
        _indexManager.Writer.AddDocument(luceneDoc);
        _indexManager.Commit();
        _indexManager.RefreshSearchers();

        _logger.LogDebug("Indexed document {DocumentId}: '{Title}'", document.Id, document.Title);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveFromIndexAsync(int documentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // Use NumericRangeQuery since ID is stored as Int32Field
        _indexManager.Writer.DeleteDocuments(
            NumericRangeQuery.NewInt32Range(
                FieldNames.Id, documentId, documentId, true, true));
        _indexManager.Commit();
        _indexManager.RefreshSearchers();

        _logger.LogDebug("Removed document {DocumentId} from search index", documentId);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<int>> SearchAsync(
        string query,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<int>();

        ct.ThrowIfCancellationRequested();

        var searchQuery = new SearchQuery { Query = query, PageSize = 10000 };
        var result = await SearchDocumentsAsync(searchQuery, ct);
        return result.DocumentIds;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<string>> AutocompleteAsync(
        string prefix,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return Array.Empty<string>();

        ct.ThrowIfCancellationRequested();

        var suggestions = new HashSet<string>();
        var searcher = _indexManager.AcquireSearcher();

        try
        {
            var reader = searcher.IndexReader;
            var lowerPrefix = prefix.ToLowerInvariant();

            // Search in title and content fields for autocomplete
            suggestions.UnionWith(
                GetSuggestionsForField(reader, FieldNames.Title, lowerPrefix));
            suggestions.UnionWith(
                GetSuggestionsForField(reader, FieldNames.Content, lowerPrefix));

            _logger.LogDebug(
                "Autocomplete for '{Prefix}' returned {Count} suggestions",
                prefix, suggestions.Count);
        }
        finally
        {
            _indexManager.ReleaseSearcher(searcher);
        }

        return suggestions;
    }

    /// <summary>
    /// Collects autocomplete suggestions from a specific indexed field.
    /// Uses the Lucene Terms API to iterate over indexed terms matching the prefix.
    /// </summary>
    private static HashSet<string> GetSuggestionsForField(
        IndexReader reader, string field, string lowerPrefix)
    {
        var result = new HashSet<string>();
        var terms = MultiFields.GetTerms(reader, field);
        if (terms == null)
            return result;

        // Terms implements IEnumerable<TermsEnum> in Lucene.NET 4.8.
        // TermsEnum has a Term property (BytesRef) for the current term value.
        const int maxSuggestionsPerField = 10;
        var count = 0;

        foreach (var tenum in terms)
        {
            var termBytes = tenum.Term;
            if (termBytes == null || termBytes.Length == 0)
                continue;

            var termText = termBytes.Utf8ToString();
            if (string.IsNullOrWhiteSpace(termText))
                continue;

            if (!termText.StartsWith(lowerPrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            result.Add(termText);
            count++;
            if (count >= maxSuggestionsPerField)
                break;
        }

        return result;
    }

    // ────────────────────────────────────────────────────────────────
    //  Advanced search with pagination and filters
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Performs a full-text search with the specified <paramref name="searchQuery"/>
    /// parameters including filters, sorting, and pagination.
    /// </summary>
    /// <param name="searchQuery">The search query with filters and pagination.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated search result with matching document IDs.</returns>
    public async Task<SearchResult> SearchDocumentsAsync(
        SearchQuery searchQuery,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(searchQuery);

        var errors = searchQuery.Validate();
        if (errors.Count > 0)
        {
            var joined = string.Join("; ", errors);
            _logger.LogError("Invalid search query: {Errors}", joined);
            throw new ArgumentException($"Invalid search query: {joined}", nameof(searchQuery));
        }

        ct.ThrowIfCancellationRequested();

        var searcher = _indexManager.AcquireSearcher();

        try
        {
            // Build the Lucene query
            var query = BuildQuery(searchQuery);
            var sort = BuildSort(searchQuery);

            // Execute the search
            var topDocs = sort != null
                ? searcher.Search(query, searchQuery.Page * searchQuery.PageSize, sort)
                : searcher.Search(query, searchQuery.Page * searchQuery.PageSize);

            // Calculate pagination
            var totalCount = topDocs.TotalHits;
            var startIndex = (searchQuery.Page - 1) * searchQuery.PageSize;
            var endIndex = Math.Min(startIndex + searchQuery.PageSize, topDocs.ScoreDocs.Length);

            var documentIds = new List<int>(endIndex - startIndex);

            for (int i = startIndex; i < endIndex; i++)
            {
                var luceneDoc = searcher.Doc(topDocs.ScoreDocs[i].Doc);
                var idField = luceneDoc.GetField(FieldNames.Id);
                if (idField?.GetInt32Value() is int idValue)
                {
                    documentIds.Add(idValue);
                }
            }

            _logger.LogDebug(
                "Search for '{Query}' returned {TotalCount} total results, page {Page}/{PageSize}",
                searchQuery.Query, totalCount, searchQuery.Page, searchQuery.PageSize);

            return new SearchResult
            {
                DocumentIds = documentIds,
                TotalCount = totalCount,
                Page = searchQuery.Page,
                PageSize = searchQuery.PageSize
            };
        }
        finally
        {
            _indexManager.ReleaseSearcher(searcher);
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  Index management helpers
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a Lucene <see cref="global::Lucene.Net.Documents.Document"/> from a domain
    /// <see cref="Document"/> entity. Maps all fields required for search and filtering.
    /// </summary>
    /// <param name="document">The domain document entity to index.</param>
    /// <returns>A Lucene document ready for indexing.</returns>
    public global::Lucene.Net.Documents.Document CreateLuceneDocument(Document document)
    {
        var luceneDoc = new global::Lucene.Net.Documents.Document();

        // ── Identifier ──────────────────────────────────────────
        luceneDoc.Add(new Int32Field(
            FieldNames.Id,
            document.Id,
            Field.Store.YES));

        // ── Full-text fields ────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(document.Title))
        {
            luceneDoc.Add(new TextField(
                FieldNames.Title,
                document.Title,
                Field.Store.YES));
        }

        if (!string.IsNullOrWhiteSpace(document.Content))
        {
            luceneDoc.Add(new TextField(
                FieldNames.Content,
                document.Content,
                Field.Store.YES));
        }

        // ── Filter/Sort fields ──────────────────────────────────
        if (document.CorrespondentId.HasValue)
        {
            luceneDoc.Add(new Int32Field(
                FieldNames.CorrespondentId,
                document.CorrespondentId.Value,
                Field.Store.YES));
        }

        if (document.Correspondent?.Name != null)
        {
            luceneDoc.Add(new StringField(
                FieldNames.Correspondent,
                document.Correspondent.Name,
                Field.Store.YES));
        }

        if (document.DocumentTypeId.HasValue)
        {
            luceneDoc.Add(new Int32Field(
                FieldNames.DocumentTypeId,
                document.DocumentTypeId.Value,
                Field.Store.YES));
        }

        if (document.DocumentType?.Name != null)
        {
            luceneDoc.Add(new StringField(
                FieldNames.DocumentType,
                document.DocumentType.Name,
                Field.Store.YES));
        }

        // ── Tags (multi-valued) ─────────────────────────────────
        foreach (var tag in document.Tags)
        {
            luceneDoc.Add(new Int32Field(
                FieldNames.TagIds,
                tag.Id,
                Field.Store.YES));

            luceneDoc.Add(new StringField(
                FieldNames.Tags,
                tag.Name,
                Field.Store.YES));
        }

        // ── Date fields (stored as milliseconds since epoch) ────
        if (document.Created.HasValue)
        {
            luceneDoc.Add(new Int64Field(
                FieldNames.Created,
                ToEpochMilliseconds(document.Created.Value),
                Field.Store.YES));
        }

        luceneDoc.Add(new Int64Field(
            FieldNames.Added,
            ToEpochMilliseconds(document.Added),
            Field.Store.YES));

        // ── Metadata fields for filtering ───────────────────────
        if (document.StoragePath != null)
        {
            luceneDoc.Add(new StringField(
                FieldNames.StoragePath,
                document.StoragePath,
                Field.Store.YES));
        }

        if (document.OwnerId.HasValue)
        {
            luceneDoc.Add(new Int32Field(
                FieldNames.OwnerId,
                document.OwnerId.Value,
                Field.Store.YES));
        }

        if (document.ArchiveSerialNumber.HasValue)
        {
            luceneDoc.Add(new Int32Field(
                FieldNames.ArchiveSerialNumber,
                document.ArchiveSerialNumber.Value,
                Field.Store.YES));
        }

        return luceneDoc;
    }

    // ────────────────────────────────────────────────────────────────
    //  Query building
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a Lucene <see cref="Query"/> from the <see cref="SearchQuery"/> parameters.
    /// Combines the full-text query with all active filters using BooleanQuery.
    /// </summary>
    internal Query BuildQuery(SearchQuery searchQuery)
    {
        var booleanQuery = new BooleanQuery();

        // ── Full-text query ──────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(searchQuery.Query))
        {
            var parser = new MultiFieldQueryParser(
                LuceneVersion.LUCENE_48,
                new[] { FieldNames.Title, FieldNames.Content },
                _indexManager.Analyzer)
            {
                DefaultOperator = QueryParserBase.AND_OPERATOR
            };

            try
            {
                var parsedQuery = parser.Parse(searchQuery.Query);
                booleanQuery.Add(parsedQuery, Occur.MUST);
            }
            catch (ParseException ex)
            {
                _logger.LogWarning(ex,
                    "Failed to parse search query '{Query}', falling back to fuzzy term query",
                    searchQuery.Query);

                var fuzzyQuery = new FuzzyQuery(
                    new Term(FieldNames.Content, searchQuery.Query));
                booleanQuery.Add(fuzzyQuery, Occur.MUST);
            }
        }
        else
        {
            // No text query — match all documents
            booleanQuery.Add(new MatchAllDocsQuery(), Occur.MUST);
        }

        // ── Filters ──────────────────────────────────────────────
        // In Lucene 4.8, there's no Occur.FILTER.
        // Uses Occur.MUST instead — filter queries are numeric range queries
        // which have minimal scoring impact.

        // Filter by tag IDs (AND: document must have ALL specified tags)
        if (searchQuery.TagIds is { Count: > 0 })
        {
            foreach (var tagId in searchQuery.TagIds)
            {
                booleanQuery.Add(
                    NumericRangeQuery.NewInt32Range(
                        FieldNames.TagIds, tagId, tagId, true, true),
                    Occur.MUST);
            }
        }

        // Filter by correspondent ID
        if (searchQuery.CorrespondentId.HasValue)
        {
            booleanQuery.Add(
                NumericRangeQuery.NewInt32Range(
                    FieldNames.CorrespondentId,
                    searchQuery.CorrespondentId.Value,
                    searchQuery.CorrespondentId.Value,
                    true, true),
                Occur.MUST);
        }

        // Filter by document type ID
        if (searchQuery.DocumentTypeId.HasValue)
        {
            booleanQuery.Add(
                NumericRangeQuery.NewInt32Range(
                    FieldNames.DocumentTypeId,
                    searchQuery.DocumentTypeId.Value,
                    searchQuery.DocumentTypeId.Value,
                    true, true),
                Occur.MUST);
        }

        // Filter by inbox status
        if (searchQuery.IsInbox.HasValue)
        {
            booleanQuery.Add(
                NumericRangeQuery.NewInt32Range(
                    FieldNames.IsInbox,
                    searchQuery.IsInbox.Value ? 1 : 0,
                    searchQuery.IsInbox.Value ? 1 : 0,
                    true, true),
                Occur.MUST);
        }

        // Filter by storage path
        if (!string.IsNullOrWhiteSpace(searchQuery.StoragePath))
        {
            booleanQuery.Add(
                new TermQuery(new Term(FieldNames.StoragePath, searchQuery.StoragePath)),
                Occur.MUST);
        }

        // Filter by owner ID
        if (searchQuery.OwnerId.HasValue)
        {
            booleanQuery.Add(
                NumericRangeQuery.NewInt32Range(
                    FieldNames.OwnerId,
                    searchQuery.OwnerId.Value,
                    searchQuery.OwnerId.Value,
                    true, true),
                Occur.MUST);
        }

        // Filter by archive serial number
        if (searchQuery.ArchiveSerialNumber.HasValue)
        {
            booleanQuery.Add(
                NumericRangeQuery.NewInt32Range(
                    FieldNames.ArchiveSerialNumber,
                    searchQuery.ArchiveSerialNumber.Value,
                    searchQuery.ArchiveSerialNumber.Value,
                    true, true),
                Occur.MUST);
        }

        // ── Date range filters ──────────────────────────────────
        if (searchQuery.CreatedAfter.HasValue)
        {
            booleanQuery.Add(
                NumericRangeQuery.NewInt64Range(
                    FieldNames.Created,
                    ToEpochMilliseconds(searchQuery.CreatedAfter.Value),
                    long.MaxValue,
                    true, true),
                Occur.MUST);
        }

        if (searchQuery.CreatedBefore.HasValue)
        {
            booleanQuery.Add(
                NumericRangeQuery.NewInt64Range(
                    FieldNames.Created,
                    long.MinValue,
                    ToEpochMilliseconds(searchQuery.CreatedBefore.Value),
                    true, true),
                Occur.MUST);
        }

        if (searchQuery.AddedAfter.HasValue)
        {
            booleanQuery.Add(
                NumericRangeQuery.NewInt64Range(
                    FieldNames.Added,
                    ToEpochMilliseconds(searchQuery.AddedAfter.Value),
                    long.MaxValue,
                    true, true),
                Occur.MUST);
        }

        if (searchQuery.AddedBefore.HasValue)
        {
            booleanQuery.Add(
                NumericRangeQuery.NewInt64Range(
                    FieldNames.Added,
                    long.MinValue,
                    ToEpochMilliseconds(searchQuery.AddedBefore.Value),
                    true, true),
                Occur.MUST);
        }

        return booleanQuery;
    }

    /// <summary>
    /// Builds a Lucene <see cref="Sort"/> from the <see cref="SearchQuery"/> sort parameters.
    /// Returns null if no explicit sort is specified (uses relevance scoring).
    /// </summary>
    internal Sort? BuildSort(SearchQuery searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery.SortBy))
            return null;

        var reverse = !searchQuery.SortAscending;

        var fieldName = searchQuery.SortBy.ToLowerInvariant() switch
        {
            "created" => FieldNames.Created,
            "added" => FieldNames.Added,
            "title" => FieldNames.Title,
            _ => null
        };

        if (fieldName == null)
        {
            _logger.LogWarning("Unknown sort field '{SortBy}', falling back to relevance",
                searchQuery.SortBy);
            return null;
        }

        // Use appropriate sort type based on field
        if (fieldName == FieldNames.Created || fieldName == FieldNames.Added)
        {
            return new Sort(new SortField(fieldName, SortFieldType.INT64, reverse));
        }

        return new Sort(new SortField(fieldName, SortFieldType.STRING, reverse));
    }

    // ────────────────────────────────────────────────────────────────
    //  Utility
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts a <see cref="DateTime"/> to milliseconds since Unix epoch.
    /// </summary>
    public static long ToEpochMilliseconds(DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Converts milliseconds since Unix epoch back to a <see cref="DateTime"/>.
    /// </summary>
    public static DateTime FromEpochMilliseconds(long milliseconds)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
    }

    /// <summary>
    /// Optimizes the search index by merging all segments into one.
    /// Should be called periodically (e.g., via a recurring job).
    /// </summary>
    public void OptimizeIndex()
    {
        _indexManager.Optimize();
        _logger.LogInformation("Search index optimization completed");
    }

    /// <summary>
    /// Clears the entire search index.
    /// </summary>
    public void ClearIndex()
    {
        _indexManager.Clear();
        _logger.LogInformation("Search index cleared");
    }

    /// <summary>
    /// Disposes the search backend and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _indexManager?.Dispose();
    }

    /// <summary>
    /// Constant field names used in the Lucene index.
    /// </summary>
    public static class FieldNames
    {
        public const string Id = "id";
        public const string Title = "title";
        public const string Content = "content";
        public const string Correspondent = "correspondent";
        public const string CorrespondentId = "correspondent_id";
        public const string DocumentType = "document_type";
        public const string DocumentTypeId = "document_type_id";
        public const string TagIds = "tag_ids";
        public const string Tags = "tags";
        public const string Created = "created";
        public const string Added = "added";
        public const string IsInbox = "is_inbox";
        public const string OwnerId = "owner_id";
        public const string StoragePath = "storage_path";
        public const string ArchiveSerialNumber = "archive_serial_number";
    }
}
