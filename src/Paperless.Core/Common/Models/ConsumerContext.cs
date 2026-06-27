using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Common.Models;

/// <summary>
/// Context passed through the consumer pipeline stages.
/// Carries the consumable document, the created document entity, and accumulated state during processing.
/// </summary>
public sealed record ConsumerContext
{
    /// <summary>
    /// The document being consumed (input to the pipeline).
    /// </summary>
    public required ConsumableDocument Document { get; init; }

    /// <summary>
    /// The created domain <see cref="Documents.Entities.Document"/> entity after it has been persisted.
    /// Populated by the consume stage and available to post-consume plugins.
    /// </summary>
    public Documents.Entities.Document? DocumentEntity { get; init; }

    /// <summary>
    /// The path to the working directory for intermediate files.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Tracks which pipeline stages have completed and whether they succeeded.
    /// Key is the stage name (e.g., "PreConsume", "Consume", "PostConsume"),
    /// value is true if the stage completed successfully.
    /// </summary>
    public Dictionary<string, bool> StageStatuses { get; init; } = new();

    /// <summary>
    /// Arbitrary metadata accumulated by pipeline stages.
    /// </summary>
    public Dictionary<string, object?> Metadata { get; init; } = new();
}
