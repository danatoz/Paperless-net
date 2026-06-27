namespace Paperless.Core.Documents.Entities;

/// <summary>
/// Singleton entity storing application-wide configuration settings.
/// Maps to the ApplicationConfiguration model from the original paperless-ngx data model.
/// There should always be exactly one row in the database for this entity.
/// </summary>
public class ApplicationConfiguration : BaseEntity
{
    /// <summary>
    /// The number of days to retain documents in the trash before permanent deletion.
    /// </summary>
    public int? TrashRetentionDays { get; set; }

    /// <summary>
    /// Maximum image pixels for the consumed document (width or height).
    /// </summary>
    public int? ConsumeMaxImagePixels { get; set; }

    /// <summary>
    /// Maximum file size (in bytes) allowed for consumption.
    /// </summary>
    public long? ConsumeMaxFileSize { get; set; }

    /// <summary>
    /// Whether to substitute blank pages with the preceding page during OCR.
    /// </summary>
    public bool? OcrClean { get; set; }

    /// <summary>
    /// Whether to use continuous mode during OCR (remove uneven page borders).
    /// </summary>
    public bool? OcrCleanContinuously { get; set; }

    /// <summary>
    /// Output type for OCR processing (e.g., "pdf", "pdfa").
    /// </summary>
    public string? OcrOutputType { get; set; }

    /// <summary>
    /// Whether to skip OCR for documents that already contain text.
    /// </summary>
    public bool? OcrSkipAlreadyDone { get; set; }

    /// <summary>
    /// Language(s) to use for OCR (e.g., "eng+deu").
    /// </summary>
    public string? OcrLanguage { get; set; }

    /// <summary>
    /// The default owner for documents created via consumption.
    /// </summary>
    public int? DefaultOwnerId { get; set; }

    /// <summary>
    /// The application's timezone (e.g., "UTC", "Europe/Berlin").
    /// </summary>
    public string? Timezone { get; set; }

    /// <summary>
    /// Whether updates should be checked automatically.
    /// </summary>
    public bool? UpdateCheckingEnabled { get; set; }

    /// <summary>
    /// The PDF rendering DPI (dots per inch).
    /// </summary>
    public int? PdfDpi { get; set; }

    /// <summary>
    /// The JPEG quality percentage for thumbnail generation.
    /// </summary>
    public int? JpegQuality { get; set; }
}
