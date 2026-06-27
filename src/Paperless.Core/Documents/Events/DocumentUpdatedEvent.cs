using MediatR;

namespace Paperless.Core.Documents.Events;

/// <summary>
/// Domain event raised when a document is updated.
/// </summary>
/// <param name="DocumentId">The unique identifier of the updated document.</param>
/// <param name="ChangedFields">The set of field names that were modified.</param>
/// <param name="Timestamp">The UTC timestamp when the document was updated.</param>
public sealed record DocumentUpdatedEvent(
    int DocumentId,
    IReadOnlySet<string> ChangedFields,
    DateTime Timestamp) : INotification;
