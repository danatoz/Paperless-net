using Paperless.Core.Documents.Enums;

namespace Paperless.Core.Documents.ValueObjects;

/// <summary>
/// Describes the origin of a document that entered the consumer pipeline.
/// Maps to the document source tracking in documents/data_models.py.
/// </summary>
public sealed record DocumentSourceRecord
{
    /// <summary>
    /// The source type (mail, web upload, file system, barcode).
    /// </summary>
    public DocumentSource Source { get; init; }

    /// <summary>
    /// The file path on disk (for file system consumption).
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// The original filename as provided by the source.
    /// </summary>
    public string? Filename { get; init; }
}
