using MediatR;
using Microsoft.Extensions.Logging;
using Paperless.Core.Common.Interfaces;
using Paperless.Core.Documents.Events;

namespace Paperless.Infrastructure.Search;

/// <summary>
/// MediatR notification handler that synchronizes the Lucene.NET search index
/// when documents are created, updated, or deleted.
/// 
/// Listens to <see cref="DocumentCreatedEvent"/>, <see cref="DocumentUpdatedEvent"/>,
/// and <see cref="DocumentDeletedEvent"/> and updates the search index accordingly.
/// </summary>
public sealed class DocumentSearchIndexEventHandler :
    INotificationHandler<DocumentCreatedEvent>,
    INotificationHandler<DocumentUpdatedEvent>,
    INotificationHandler<DocumentDeletedEvent>
{
    private readonly LuceneSearchBackend _searchBackend;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<DocumentSearchIndexEventHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentSearchIndexEventHandler"/> class.
    /// </summary>
    /// <param name="searchBackend">The Lucene search backend.</param>
    /// <param name="documentRepository">The document repository for loading full document data.</param>
    /// <param name="logger">Logger instance.</param>
    public DocumentSearchIndexEventHandler(
        LuceneSearchBackend searchBackend,
        IDocumentRepository documentRepository,
        ILogger<DocumentSearchIndexEventHandler> logger)
    {
        _searchBackend = searchBackend ?? throw new ArgumentNullException(nameof(searchBackend));
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles document creation by indexing the new document in the search index.
    /// </summary>
    public async Task Handle(DocumentCreatedEvent notification, CancellationToken ct)
    {
        try
        {
            var document = await _documentRepository.GetByIdAsync(notification.DocumentId, ct);
            if (document == null)
            {
                _logger.LogWarning(
                    "DocumentCreatedEvent: Document {DocumentId} not found in database, skipping index",
                    notification.DocumentId);
                return;
            }

            await _searchBackend.IndexDocumentAsync(document, ct);
            _logger.LogDebug(
                "Indexed document {DocumentId} after creation event",
                notification.DocumentId);
        }
        catch (Exception ex)
        {
            // Log but don't throw — index sync should not break the main operation
            _logger.LogError(ex,
                "Failed to index document {DocumentId} after creation event",
                notification.DocumentId);
        }
    }

    /// <summary>
    /// Handles document updates by re-indexing the document in the search index.
    /// </summary>
    public async Task Handle(DocumentUpdatedEvent notification, CancellationToken ct)
    {
        try
        {
            var document = await _documentRepository.GetByIdAsync(notification.DocumentId, ct);
            if (document == null)
            {
                _logger.LogWarning(
                    "DocumentUpdatedEvent: Document {DocumentId} not found in database, removing from index",
                    notification.DocumentId);
                await _searchBackend.RemoveFromIndexAsync(notification.DocumentId, ct);
                return;
            }

            await _searchBackend.IndexDocumentAsync(document, ct);
            _logger.LogDebug(
                "Re-indexed document {DocumentId} after update event (changed fields: {Fields})",
                notification.DocumentId,
                string.Join(", ", notification.ChangedFields));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to re-index document {DocumentId} after update event",
                notification.DocumentId);
        }
    }

    /// <summary>
    /// Handles document deletion by removing the document from the search index.
    /// </summary>
    public async Task Handle(DocumentDeletedEvent notification, CancellationToken ct)
    {
        try
        {
            await _searchBackend.RemoveFromIndexAsync(notification.DocumentId, ct);
            _logger.LogDebug(
                "Removed document {DocumentId} from search index after deletion event",
                notification.DocumentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to remove document {DocumentId} from index after deletion event",
                notification.DocumentId);
        }
    }
}
