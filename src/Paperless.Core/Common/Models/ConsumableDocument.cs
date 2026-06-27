using Paperless.Core.Documents.ValueObjects;

namespace Paperless.Core.Common.Models;

/// <summary>
/// Represents a document that is being consumed by the consumer pipeline.
/// Contains the original file data and optional metadata overrides.
/// Maps to the ConsumableDocument dataclass from paperless-ngx data_models.py.
/// </summary>
public sealed record ConsumableDocument
{
    /// <summary>
    /// The path to the original file on disk (temporary location before processing).
    /// </summary>
    public string? OriginalFilePath { get; init; }

    /// <summary>
    /// The stream containing the original file content.
    /// </summary>
    public Stream? FileStream { get; init; }

    /// <summary>
    /// The original filename as uploaded.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Optional override for the correspondent.
    /// </summary>
    public int? CorrespondentId { get; init; }

    /// <summary>
    /// Optional override for the document type.
    /// </summary>
    public int? DocumentTypeId { get; init; }

    /// <summary>
    /// Optional override for the tags to assign.
    /// </summary>
    public IReadOnlyCollection<int>? TagIds { get; init; }

    /// <summary>
    /// Optional override for the document creation date.
    /// </summary>
    public DateTime? Created { get; init; }

    /// <summary>
    /// The date the document was added to the system.
    /// </summary>
    public DateTime Added { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Optional ID of the owner user.
    /// </summary>
    public int? OwnerId { get; init; }

    /// <summary>
    /// Optional metadata overrides for the consumption process.
    /// </summary>
    public DocumentMetadataOverrides? OverrideMetadata { get; init; }

    /// <summary>
    /// SHA-256 checksum of the original file, used for deduplication.
    /// </summary>
    public string? Checksum { get; init; }
}
