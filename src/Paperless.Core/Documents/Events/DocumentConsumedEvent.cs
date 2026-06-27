using MediatR;

namespace Paperless.Core.Documents.Events;

/// <summary>
/// Domain event raised when a document has completed the consumer pipeline.
/// </summary>
/// <param name="DocumentId">The unique identifier of the consumed document.</param>
/// <param name="Status">The consumption status (e.g., "success", "failed").</param>
/// <param name="Timestamp">The UTC timestamp when consumption completed.</param>
public sealed record DocumentConsumedEvent(
    int DocumentId,
    string Status,
    DateTime Timestamp) : INotification;
