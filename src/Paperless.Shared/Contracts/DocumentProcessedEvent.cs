using MediatR;

namespace Paperless.Shared.Contracts;

/// <summary>
/// Event raised when a document has been processed through the consumer pipeline.
/// </summary>
/// <param name="DocumentId">The unique identifier of the processed document.</param>
/// <param name="Status">The processing status (e.g., "success", "failed").</param>
/// <param name="Timestamp">The UTC timestamp when the processing completed.</param>
public sealed record DocumentProcessedEvent(
    int DocumentId,
    string Status,
    DateTime Timestamp) : INotification;
