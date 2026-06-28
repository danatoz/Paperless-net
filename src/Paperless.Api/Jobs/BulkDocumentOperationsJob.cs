using Hangfire;
using Microsoft.Extensions.Logging;
using Paperless.Api.Dto.Requests;

namespace Paperless.Api.Jobs;

/// <summary>
/// Placeholder Hangfire job for bulk document operations.
/// These methods serve as the target for <see cref="IBackgroundJobClient"/>
/// calls from <see cref="Controllers.DocumentsController"/>
/// and will be fully implemented in M4-03, M4-04, and M4-07.
///
/// <para>Current behaviour: logs the operation and does minimal processing.
/// Real implementations will be added in the Background Jobs milestone.</para>
/// </summary>
public class BulkDocumentOperationsJob
{
    private readonly ILogger<BulkDocumentOperationsJob> _logger;

    public BulkDocumentOperationsJob(ILogger<BulkDocumentOperationsJob> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Applies a bulk edit operation (set correspondent, tags, document type, etc.).
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public Task ExecuteBulkEditAsync(
        BulkEditRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Bulk edit: {Method} on {Count} document(s) — placeholder, will be implemented in M4-03",
            request.Method,
            request.DocumentIds.Count);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Soft-deletes multiple documents.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public Task ExecuteDeleteAsync(
        DocumentSetRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Bulk delete: {Count} document(s) — placeholder, will be implemented in M4-03",
            request.DocumentIds.Count);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Reprocesses (re-runs OCR on) multiple documents.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public Task ExecuteReprocessAsync(
        DocumentSetRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Reprocess: {Count} document(s) — placeholder, will be implemented in M4-03",
            request.DocumentIds.Count);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Rotates pages in multiple PDF documents.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public Task ExecuteRotateAsync(
        RotateRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Rotate: {Count} document(s) by {Rotation}° — placeholder, will be implemented in M4-04",
            request.DocumentIds.Count,
            request.Rotation);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Merges multiple documents into a single PDF.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public Task ExecuteMergeAsync(
        MergeRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Merge: {Count} document(s) — placeholder, will be implemented in M4-04",
            request.DocumentIds.Count);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes password protection from multiple PDF documents.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public Task ExecuteRemovePasswordAsync(
        RemovePasswordRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Remove password: {Count} document(s) — placeholder, will be implemented in M4-04",
            request.DocumentIds.Count);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a ZIP archive containing multiple documents for bulk download.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public Task ExecuteBulkDownloadAsync(
        BulkDownloadRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Bulk download: {Count} document(s), content={Content} — placeholder, will be implemented in M4-07",
            request.DocumentIds.Count,
            request.Content);

        return Task.CompletedTask;
    }
}
