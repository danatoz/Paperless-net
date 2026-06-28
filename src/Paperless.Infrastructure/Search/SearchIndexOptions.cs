namespace Paperless.Infrastructure.Search;

/// <summary>
/// Configuration options for the Lucene.NET search index.
/// Bound from appsettings.json section "SearchIndex" via IOptions.
/// </summary>
public class SearchIndexOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "SearchIndex";

    /// <summary>
    /// Filesystem directory where the Lucene index is stored.
    /// Defaults to "./search_index" relative to the application working directory.
    /// </summary>
    public string IndexDirectory { get; set; } = "./search_index";

    /// <summary>
    /// The Lucene analyzer type to use for text analysis.
    /// Supported values: "standard", "simple", "whitespace", "keyword".
    /// Defaults to "standard".
    /// </summary>
    public string AnalyzerType { get; set; } = "standard";

    /// <summary>
    /// The RAM buffer size for the IndexWriter in megabytes.
    /// Larger values improve indexing speed at the cost of memory.
    /// </summary>
    public int RamBufferSizeMb { get; set; } = 16;

    /// <summary>
    /// Whether to use compound file format for the index.
    /// Compound files reduce the number of file handles but may slow down merging.
    /// </summary>
    public bool UseCompoundFile { get; set; } = true;
}
