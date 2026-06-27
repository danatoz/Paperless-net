using MediatR;

namespace Paperless.Core.Documents.Events;

/// <summary>
/// Domain event raised when a new document is created.
/// </summary>
/// <param name="DocumentId">The unique identifier of the created document.</param>
/// <param name="Timestamp">The UTC timestamp when the document was created.</param>
public sealed record DocumentCreatedEvent(
    int DocumentId,
    DateTime Timestamp) : INotification;
