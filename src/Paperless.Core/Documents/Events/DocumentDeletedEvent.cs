using MediatR;

namespace Paperless.Core.Documents.Events;

/// <summary>
/// Domain event raised when a document is soft-deleted.
/// </summary>
/// <param name="DocumentId">The unique identifier of the deleted document.</param>
/// <param name="Timestamp">The UTC timestamp when the document was deleted.</param>
public sealed record DocumentDeletedEvent(
    int DocumentId,
    DateTime Timestamp) : INotification;
