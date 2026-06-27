using Paperless.Shared.Abstractions;

namespace Paperless.Core.Common.Models;

/// <summary>
/// Result returned by a consumer pipeline stage or the full pipeline.
/// </summary>
public sealed record ConsumerResult
{
    /// <summary>
    /// Whether the stage/pipeline completed successfully.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// The ID of the document that was created/processed (if applicable).
    /// </summary>
    public int? DocumentId { get; init; }

    /// <summary>
    /// Error details if the stage/pipeline failed.
    /// </summary>
    public Error? Error { get; init; }

    /// <summary>
    /// Informational messages from the processing stage.
    /// </summary>
    public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ConsumerResult Success(int? documentId = null, params string[] messages) =>
        new() { IsSuccess = true, DocumentId = documentId, Messages = messages };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ConsumerResult Failure(Error error, params string[] messages) =>
        new() { IsSuccess = false, Error = error, Messages = messages };
}
