using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paperless.Api.Dto.Requests;
using Paperless.Api.Dto.Responses;
using Paperless.Api.Jobs;
using Paperless.Core.Common.Interfaces;
using Paperless.Core.Common.Specifications;
using Paperless.Core.Documents.Entities;
using Paperless.Infrastructure.Search;

namespace Paperless.Api.Controllers;

/// <summary>
/// REST API controller for document CRUD, file access, and bulk operations.
/// Maps to the paperless-ngx <c>UnifiedSearchViewSet</c> and document endpoints.
///
/// <para>All endpoints are under <c>/api/documents</c> (except <c>post_document</c>
/// which is at <c>/api/documents/post_document/</c> — implemented in M2-12).</para>
/// </summary>
[ApiController]
[Route("/api/documents")]
[Authorize(Policy = "UserOrAdmin")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly ISearchBackend _searchBackend;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentRepository documentRepository,
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        ISearchBackend searchBackend,
        IBackgroundJobClient backgroundJobClient,
        ILogger<DocumentsController> logger)
    {
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _searchBackend = searchBackend;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/documents/  —  List / search / filter documents
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paginated, filtered list of documents.
    /// Supports full-text search, filtering by correspondent/tags/document_type/date,
    /// sorting, and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<DocumentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] DocumentFilterRequest filter,
        CancellationToken ct)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        // ── Step 1: If there's a search query, use ISearchBackend ──
        // Otherwise build an EF Core specification for filtering.
        IReadOnlyCollection<int>? searchResultIds = null;

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var searchQuery = new SearchQuery
            {
                Query = filter.Query,
                Page = page,
                PageSize = pageSize,
                CorrespondentId = filter.Correspondent,
                DocumentTypeId = filter.DocumentType,
                TagIds = filter.ParseTagIds(),
                CreatedAfter = filter.CreatedAfter,
                CreatedBefore = filter.CreatedBefore,
                AddedAfter = filter.AddedAfter,
                AddedBefore = filter.AddedBefore,
                SortBy = ParseSortField(filter.Ordering),
                SortAscending = !IsDescendingOrdering(filter.Ordering)
            };

            // Use Infrastructure's LuceneSearchBackend directly for the advanced search
            // Since we have it injected as ISearchBackend, we use the SearchAsync method
            // to get matching IDs, then apply pagination/filtering in-memory.
            var matchedIds = await _searchBackend.SearchAsync(searchQuery.Query, ct);
            searchResultIds = matchedIds;

            if (matchedIds.Count == 0)
            {
                // No results from search — return empty page
                return Ok(BuildEmptyPage<DocumentResponse>(page, pageSize));
            }
        }

        // ── Step 2: Build EF Core specification for additional filtering ──
        var spec = new Specification<Document>();

        // Apply search result IDs as a filter
        if (searchResultIds is { Count: > 0 })
        {
            var idSet = new HashSet<int>(searchResultIds);
            spec.Where(d => idSet.Contains(d.Id));
        }

        // Apply additional EF Core filters (non-search filters)
        if (filter.Correspondent.HasValue)
        {
            var existingCriteria = spec.Criteria;
            spec.Where(d =>
                d.CorrespondentId == filter.Correspondent.Value &&
                (existingCriteria == null || existingCriteria.Compile().Invoke(d)));
        }

        if (filter.DocumentType.HasValue)
        {
            var existingCriteria = spec.Criteria;
            spec.Where(d =>
                d.DocumentTypeId == filter.DocumentType.Value &&
                (existingCriteria == null || existingCriteria.Compile().Invoke(d)));
        }

        // Apply tag filters (AND — all specified tags must be present)
        var tagIds = filter.ParseTagIds();
        if (tagIds is { Count: > 0 })
        {
            var tagSet = new HashSet<int>(tagIds);
            var existingCriteria = spec.Criteria;
            spec.Where(d =>
                d.Tags.Any(t => tagSet.Contains(t.Id)) &&
                (existingCriteria == null || existingCriteria.Compile().Invoke(d)));
        }

        // Apply date range filters
        if (filter.CreatedAfter.HasValue)
        {
            var date = filter.CreatedAfter.Value;
            var existingCriteria = spec.Criteria;
            spec.Where(d =>
                d.Created >= date &&
                (existingCriteria == null || existingCriteria.Compile().Invoke(d)));
        }

        if (filter.CreatedBefore.HasValue)
        {
            var date = filter.CreatedBefore.Value;
            var existingCriteria = spec.Criteria;
            spec.Where(d =>
                d.Created <= date &&
                (existingCriteria == null || existingCriteria.Compile().Invoke(d)));
        }

        // ── Step 3: Eager-load related entities ──
        spec.Include(d => d.Correspondent);
        spec.Include(d => d.DocumentType);
        spec.Include(d => d.Tags);
        spec.Include(d => d.CustomFields);

        // ── Step 4: Apply sorting ──
        ApplyOrdering(spec, filter.Ordering);

        // ── Step 5: Apply pagination ──
        spec.ApplyPaging(page, pageSize);

        // ── Step 6: Execute query ──
        var pagedResult = await _documentRepository.GetAllAsync(spec, ct);

        // ── Step 7: Get all matching IDs for the "all" field ──
        var allSpec = new Specification<Document>();
        // Copy filter criteria but no pagination
        if (spec.Criteria != null)
            allSpec.Where(spec.Criteria);
        allSpec.Include(d => d.Tags);

        var allDocuments = await _documentRepository.GetAllAsync(allSpec, ct);
        var allIds = allDocuments.Items.Where(d => !d.IsDeleted).Select(d => d.Id).ToList();

        // ── Step 8: Build response ──
        var response = BuildPaginatedResponse(
            pagedResult.Items.Select(DocumentResponse.FromEntity).ToList().AsReadOnly(),
            pagedResult.TotalCount,
            page,
            pageSize,
            allIds.AsReadOnly());

        return Ok(response);
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/documents/  —  Create a new document
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new document record.
    /// For file upload, use POST /api/documents/post_document/ (M2-12).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateDocumentRequest request,
        CancellationToken ct)
    {
        var document = new Document
        {
            Title = request.Title ?? "Untitled",
            Content = request.Content,
            CorrespondentId = request.CorrespondentId,
            DocumentTypeId = request.DocumentTypeId,
            Created = request.Created ?? DateTime.UtcNow,
            Added = DateTime.UtcNow,
            Modified = DateTime.UtcNow,
            ArchiveSerialNumber = request.ArchiveSerialNumber
        };

        // Attach tags if provided
        if (request.TagIds is { Count: > 0 })
        {
            // We attach the tag IDs without loading them from DB.
            // EF Core will handle the relationship if the tags exist.
            foreach (var tagId in request.TagIds)
            {
                document.Tags.Add(new Tag { Id = tagId });
            }
        }

        var created = await _documentRepository.AddAsync(document, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created document {DocumentId}: '{Title}'",
            created.Id, created.Title);

        var response = DocumentResponse.FromEntity(created);

        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, response);
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/documents/{id}/  —  Document detail
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the full detail of a single document by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        int id,
        CancellationToken ct)
    {
        var document = await _documentRepository.GetByIdAsync(id, ct);

        if (document is null || document.IsDeleted)
        {
            return NotFound(new { detail = $"Document with id {id} not found." });
        }

        return Ok(DocumentResponse.FromEntity(document));
    }

    // ────────────────────────────────────────────────────────────────
    //  PUT /api/documents/{id}/  —  Full update
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fully replaces a document's metadata.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAsync(
        int id,
        [FromBody] UpdateDocumentRequest request,
        CancellationToken ct)
    {
        var document = await _documentRepository.GetByIdAsync(id, ct);

        if (document is null || document.IsDeleted)
        {
            return NotFound(new { detail = $"Document with id {id} not found." });
        }

        // Apply full update
        document.Title = request.Title;
        document.Content = request.Content;
        document.CorrespondentId = request.CorrespondentId;
        document.DocumentTypeId = request.DocumentTypeId;
        document.Created = request.Created;
        document.ArchiveSerialNumber = request.ArchiveSerialNumber;
        document.Modified = DateTime.UtcNow;

        // Update tags
        document.Tags.Clear();
        foreach (var tagId in request.TagIds)
        {
            document.Tags.Add(new Tag { Id = tagId });
        }

        _documentRepository.Update(document);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Updated document {DocumentId}", document.Id);

        // Reload to get fresh navigation properties
        document = await _documentRepository.GetByIdAsync(id, ct);

        return Ok(DocumentResponse.FromEntity(document!));
    }

    // ────────────────────────────────────────────────────────────────
    //  PATCH /api/documents/{id}/  —  Partial update
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Partially updates a document's metadata. Only provided fields are changed.
    /// </summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchAsync(
        int id,
        [FromBody] PatchDocumentRequest request,
        CancellationToken ct)
    {
        var document = await _documentRepository.GetByIdAsync(id, ct);

        if (document is null || document.IsDeleted)
        {
            return NotFound(new { detail = $"Document with id {id} not found." });
        }

        // Apply partial update (only non-null fields)
        if (request.Title is not null)
            document.Title = request.Title;

        if (request.Content is not null)
            document.Content = request.Content;

        if (request.CorrespondentId.HasValue)
            document.CorrespondentId = request.CorrespondentId;

        if (request.DocumentTypeId.HasValue)
            document.DocumentTypeId = request.DocumentTypeId;

        if (request.Created.HasValue)
            document.Created = request.Created;

        if (request.ArchiveSerialNumber.HasValue)
            document.ArchiveSerialNumber = request.ArchiveSerialNumber;

        if (request.TagIds is not null)
        {
            document.Tags.Clear();
            foreach (var tagId in request.TagIds)
            {
                document.Tags.Add(new Tag { Id = tagId });
            }
        }

        document.Modified = DateTime.UtcNow;

        _documentRepository.Update(document);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Patched document {DocumentId}", document.Id);

        // Reload to get fresh navigation properties
        document = await _documentRepository.GetByIdAsync(id, ct);

        return Ok(DocumentResponse.FromEntity(document!));
    }

    // ────────────────────────────────────────────────────────────────
    //  DELETE /api/documents/{id}/  —  Soft delete
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Soft-deletes a document (marks as deleted, does not remove from database).
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        int id,
        CancellationToken ct)
    {
        var document = await _documentRepository.GetByIdAsync(id, ct);

        if (document is null || document.IsDeleted)
        {
            return NotFound(new { detail = $"Document with id {id} not found." });
        }

        _documentRepository.Delete(document);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Soft-deleted document {DocumentId}", id);

        return NoContent();
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/documents/{id}/download/  —  Download original
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Downloads the original uploaded file for a document.
    /// Sets Content-Disposition header for proper file download.
    /// </summary>
    [HttpGet("{id:int}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAsync(
        int id,
        CancellationToken ct)
    {
        var document = await _documentRepository.GetByIdAsync(id, ct);

        if (document is null || document.IsDeleted)
        {
            return NotFound(new { detail = $"Document with id {id} not found." });
        }

        if (string.IsNullOrWhiteSpace(document.Filename))
        {
            return NotFound(new { detail = "Document has no original file." });
        }

        try
        {
            // Document.Filename stores the relative path within the storage
            var stream = await _fileStorage.ReadAsync(document.Filename, ct);

            var contentType = GetContentType(document.Filename);
            var downloadName = document.Title ?? document.Filename;

            return File(stream, contentType, downloadName);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { detail = "Original file not found on storage." });
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/documents/{id}/preview/  —  PDF preview
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a PDF preview of the document (the archived PDF/A version).
    /// </summary>
    [HttpGet("{id:int}/preview")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewAsync(
        int id,
        CancellationToken ct)
    {
        var document = await _documentRepository.GetByIdAsync(id, ct);

        if (document is null || document.IsDeleted)
        {
            return NotFound(new { detail = $"Document with id {id} not found." });
        }

        try
        {
            // Use the storage path to locate the preview file
            var storagePath = GetPreviewPath(document);
            var stream = await _fileStorage.GetPreviewAsync(storagePath, ct);

            return File(stream, "application/pdf");
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { detail = "Preview not available for this document." });
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/documents/{id}/thumb/  —  PNG thumbnail
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a PNG thumbnail image of the document.
    /// </summary>
    [HttpGet("{id:int}/thumb")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ThumbnailAsync(
        int id,
        CancellationToken ct)
    {
        var document = await _documentRepository.GetByIdAsync(id, ct);

        if (document is null || document.IsDeleted)
        {
            return NotFound(new { detail = $"Document with id {id} not found." });
        }

        try
        {
            var thumbnailPath = GetThumbnailPath(document);
            var stream = await _fileStorage.GetThumbnailAsync(thumbnailPath, ct);

            return File(stream, "image/png");
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { detail = "Thumbnail not available for this document." });
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/documents/bulk_edit/  —  Bulk edit
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies a bulk edit operation to a set of documents.
    /// Delegates to a Hangfire background job.
    /// </summary>
    [HttpPost("bulk_edit")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult BulkEdit(
        [FromBody] BulkEditRequest request)
    {
        if (request.DocumentIds.Count == 0)
        {
            return BadRequest(new { detail = "At least one document ID is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Method))
        {
            return BadRequest(new { detail = "Method is required." });
        }

        var jobId = _backgroundJobClient.Enqueue<BulkDocumentOperationsJob>(
            job => job.ExecuteBulkEditAsync(request, CancellationToken.None));

        _logger.LogInformation(
            "Enqueued bulk edit job {JobId}: {Method} on {Count} document(s)",
            jobId, request.Method, request.DocumentIds.Count);

        return Accepted(new { job_id = jobId, detail = "Bulk edit job enqueued." });
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/documents/delete/  —  Bulk delete
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Soft-deletes multiple documents in a background job.
    /// </summary>
    [HttpPost("delete")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult BulkDelete(
        [FromBody] DocumentSetRequest request)
    {
        if (request.DocumentIds.Count == 0)
        {
            return BadRequest(new { detail = "At least one document ID is required." });
        }

        var jobId = _backgroundJobClient.Enqueue<BulkDocumentOperationsJob>(
            job => job.ExecuteDeleteAsync(request, CancellationToken.None));

        _logger.LogInformation(
            "Enqueued bulk delete job {JobId}: {Count} document(s)",
            jobId, request.DocumentIds.Count);

        return Accepted(new { job_id = jobId, detail = "Bulk delete job enqueued." });
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/documents/reprocess/  —  Reprocess documents
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reprocesses (re-runs OCR on) multiple documents in a background job.
    /// </summary>
    [HttpPost("reprocess")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Reprocess(
        [FromBody] DocumentSetRequest request)
    {
        if (request.DocumentIds.Count == 0)
        {
            return BadRequest(new { detail = "At least one document ID is required." });
        }

        var jobId = _backgroundJobClient.Enqueue<BulkDocumentOperationsJob>(
            job => job.ExecuteReprocessAsync(request, CancellationToken.None));

        _logger.LogInformation(
            "Enqueued reprocess job {JobId}: {Count} document(s)",
            jobId, request.DocumentIds.Count);

        return Accepted(new { job_id = jobId, detail = "Reprocess job enqueued." });
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/documents/rotate/  —  Rotate PDF pages
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Rotates pages in multiple PDF documents in a background job.
    /// </summary>
    [HttpPost("rotate")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Rotate(
        [FromBody] RotateRequest request)
    {
        if (request.DocumentIds.Count == 0)
        {
            return BadRequest(new { detail = "At least one document ID is required." });
        }

        var rotation = request.Rotation;
        if (rotation is not (90 or 180 or 270))
        {
            return BadRequest(new { detail = "Rotation must be 90, 180, or 270 degrees." });
        }

        var jobId = _backgroundJobClient.Enqueue<BulkDocumentOperationsJob>(
            job => job.ExecuteRotateAsync(request, CancellationToken.None));

        _logger.LogInformation(
            "Enqueued rotate job {JobId}: {Count} document(s) by {Rotation}°",
            jobId, request.DocumentIds.Count, rotation);

        return Accepted(new { job_id = jobId, detail = "Rotate job enqueued." });
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/documents/merge/  —  Merge PDFs
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Merges multiple documents into a single PDF in a background job.
    /// </summary>
    [HttpPost("merge")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Merge(
        [FromBody] MergeRequest request)
    {
        if (request.DocumentIds.Count < 2)
        {
            return BadRequest(new { detail = "At least two document IDs are required to merge." });
        }

        var jobId = _backgroundJobClient.Enqueue<BulkDocumentOperationsJob>(
            job => job.ExecuteMergeAsync(request, CancellationToken.None));

        _logger.LogInformation(
            "Enqueued merge job {JobId}: {Count} document(s)",
            jobId, request.DocumentIds.Count);

        return Accepted(new { job_id = jobId, detail = "Merge job enqueued." });
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/documents/edit_pdf/  —  Edit PDF (overlay)
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Edits a PDF document (e.g., text overlay) in a background job.
    /// Accepts multipart form with a PDF file.
    /// </summary>
    [HttpPost("edit_pdf")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult EditPdf(
        [FromForm] IFormFile? file,
        [FromForm(Name = "documents")] string? documentsCsv)
    {
        // Parse document IDs from the CSV string
        var documentIds = documentsCsv?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList() ?? [];

        if (documentIds.Count == 0)
        {
            return BadRequest(new { detail = "At least one document ID is required." });
        }

        // The actual file processing will be implemented in M4-04.
        // For now, we enqueue a job with the document IDs.
        var request = new DocumentSetRequest { DocumentIds = documentIds.AsReadOnly() };

        var jobId = _backgroundJobClient.Enqueue<BulkDocumentOperationsJob>(
            job => job.ExecuteMergeAsync(
                new MergeRequest { DocumentIds = request.DocumentIds },
                CancellationToken.None));

        _logger.LogInformation(
            "Enqueued edit_pdf job {JobId}: {Count} document(s)",
            jobId, documentIds.Count);

        return Accepted(new { job_id = jobId, detail = "Edit PDF job enqueued." });
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/documents/remove_password/  —  Remove PDF password
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Removes password protection from multiple PDF documents in a background job.
    /// </summary>
    [HttpPost("remove_password")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult RemovePassword(
        [FromBody] RemovePasswordRequest request)
    {
        if (request.DocumentIds.Count == 0)
        {
            return BadRequest(new { detail = "At least one document ID is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { detail = "Password is required." });
        }

        var jobId = _backgroundJobClient.Enqueue<BulkDocumentOperationsJob>(
            job => job.ExecuteRemovePasswordAsync(request, CancellationToken.None));

        _logger.LogInformation(
            "Enqueued remove_password job {JobId}: {Count} document(s)",
            jobId, request.DocumentIds.Count);

        return Accepted(new { job_id = jobId, detail = "Remove password job enqueued." });
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/documents/bulk_download/  —  Bulk download (ZIP)
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a ZIP archive containing multiple documents for download.
    /// Delegates to a Hangfire background job.
    /// </summary>
    [HttpPost("bulk_download")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult BulkDownload(
        [FromBody] BulkDownloadRequest request)
    {
        if (request.DocumentIds.Count == 0)
        {
            return BadRequest(new { detail = "At least one document ID is required." });
        }

        var jobId = _backgroundJobClient.Enqueue<BulkDocumentOperationsJob>(
            job => job.ExecuteBulkDownloadAsync(request, CancellationToken.None));

        _logger.LogInformation(
            "Enqueued bulk download job {JobId}: {Count} document(s), content={Content}",
            jobId, request.DocumentIds.Count, request.Content);

        return Accepted(new { job_id = jobId, detail = "Bulk download job enqueued." });
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/documents/selection_data/  —  Selection data
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns data about the current document selection for bulk operation dialogs.
    /// Provides counts and distinct values of related entities across the selection.
    /// </summary>
    [HttpGet("selection_data")]
    [ProducesResponseType(typeof(SelectionDataResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SelectionData(
        [FromQuery] string? documents,
        CancellationToken ct)
    {
        // Parse the optional documents filter
        var documentIds = documents?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        // Get the relevant documents
        var spec = new Specification<Document>();
        spec.Include(d => d.Correspondent);
        spec.Include(d => d.DocumentType);
        spec.Include(d => d.Tags);
        spec.Include(d => d.CustomFields);

        if (documentIds is { Count: > 0 })
        {
            spec.Where(d => documentIds.Contains(d.Id));
        }

        var documentsList = await _documentRepository.GetAllAsync(spec, ct);
        var nonDeletedDocs = documentsList.Items.Where(d => !d.IsDeleted).ToList();

        // Build selection data
        var selectedCorrespondents = nonDeletedDocs
            .Where(d => d.CorrespondentId.HasValue)
            .Select(d => d.CorrespondentId!.Value)
            .Distinct()
            .ToList()
            .AsReadOnly();

        var selectedTags = nonDeletedDocs
            .SelectMany(d => d.Tags.Select(t => t.Id))
            .Distinct()
            .ToList()
            .AsReadOnly();

        var selectedDocTypes = nonDeletedDocs
            .Where(d => d.DocumentTypeId.HasValue)
            .Select(d => d.DocumentTypeId!.Value)
            .Distinct()
            .ToList()
            .AsReadOnly();

        var response = new SelectionDataResponse
        {
            Selected = new SelectedDocumentsInfo
            {
                DocumentIds = nonDeletedDocs.Select(d => d.Id).ToList().AsReadOnly(),
                CorrespondentIds = selectedCorrespondents,
                TagIds = selectedTags,
                DocumentTypeIds = selectedDocTypes,
                StoragePathIds = Array.Empty<int>()
            },
            All = new AllDocumentsInfo
            {
                DocumentCount = nonDeletedDocs.Count,
                Correspondents = nonDeletedDocs
                    .Where(d => d.Correspondent != null)
                    .Select(d => d.Correspondent!)
                    .DistinctBy(c => c.Id)
                    .Select(c => new IdNamePair { Id = c.Id, Name = c.Name })
                    .ToList()
                    .AsReadOnly(),
                Tags = nonDeletedDocs
                    .SelectMany(d => d.Tags)
                    .DistinctBy(t => t.Id)
                    .Select(t => new IdNamePair { Id = t.Id, Name = t.Name })
                    .ToList()
                    .AsReadOnly(),
                DocumentTypes = nonDeletedDocs
                    .Where(d => d.DocumentType != null)
                    .Select(d => d.DocumentType!)
                    .DistinctBy(dt => dt.Id)
                    .Select(dt => new IdNamePair { Id = dt.Id, Name = dt.Name })
                    .ToList()
                    .AsReadOnly(),
                StoragePaths = Array.Empty<IdNamePair>()
            }
        };

        return Ok(response);
    }

    // ────────────────────────────────────────────────────────────────
    //  Private helpers
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds an empty paginated response (no results).
    /// </summary>
    private static PaginatedResponse<T> BuildEmptyPage<T>(int page, int pageSize)
    {
        return new PaginatedResponse<T>
        {
            Count = 0,
            Next = null,
            Previous = null,
            All = Array.Empty<int>(),
            Results = Array.Empty<T>()
        };
    }

    /// <summary>
    /// Builds a DRF-compatible paginated response.
    /// </summary>
    private PaginatedResponse<T> BuildPaginatedResponse<T>(
        IReadOnlyCollection<T> items,
        int totalCount,
        int page,
        int pageSize,
        IReadOnlyCollection<int> allIds)
    {
        var totalPages = totalCount > 0
            ? (int)Math.Ceiling(totalCount / (double)pageSize)
            : 0;

        string? BuildPageUrl(int targetPage)
        {
            if (targetPage < 1 || targetPage > totalPages)
                return null;

            var request = HttpContext.Request;
            var scheme = request.Scheme;
            var host = request.Host.Value;
            var pathBase = request.PathBase.Value ?? "";
            var path = request.Path.Value ?? "";

            var query = System.Web.HttpUtility.ParseQueryString(request.QueryString.Value ?? "");
            query["page"] = targetPage.ToString();
            query["page_size"] = pageSize.ToString();

            return $"{scheme}://{host}{pathBase}{path}?{query}";
        }

        return new PaginatedResponse<T>
        {
            Count = totalCount,
            Next = BuildPageUrl(page + 1),
            Previous = BuildPageUrl(page - 1),
            All = allIds,
            Results = items
        };
    }

    /// <summary>
    /// Applies ordering to a specification based on the DRF-style ordering parameter.
    /// Format: "field1,-field2" (prefix '-' for descending).
    /// </summary>
    private static void ApplyOrdering(Specification<Document> spec, string? ordering)
    {
        if (string.IsNullOrWhiteSpace(ordering))
        {
            // Default sort: by created date descending (newest first)
            spec.OrderByDescending(d => d.Created);
            return;
        }

        // Take the first ordering field
        var fields = ordering.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (fields.Length == 0)
        {
            spec.OrderByDescending(d => d.Created);
            return;
        }

        var firstField = fields[0];
        var descending = firstField.StartsWith('-');
        var fieldName = descending ? firstField[1..] : firstField;

        switch (fieldName.ToLowerInvariant())
        {
            case "created":
                if (descending)
                    spec.OrderByDescending(d => d.Created);
                else
                    spec.OrderByAscending(d => d.Created);
                break;
            case "added":
                if (descending)
                    spec.OrderByDescending(d => d.Added);
                else
                    spec.OrderByAscending(d => d.Added);
                break;
            case "title":
                if (descending)
                    spec.OrderByDescending(d => d.Title ?? "");
                else
                    spec.OrderByAscending(d => d.Title ?? "");
                break;
            case "correspondent":
                if (descending)
                    spec.OrderByDescending(d => d.Correspondent != null ? d.Correspondent.Name : "");
                else
                    spec.OrderByAscending(d => d.Correspondent != null ? d.Correspondent.Name : "");
                break;
            case "document_type":
                if (descending)
                    spec.OrderByDescending(d => d.DocumentType != null ? d.DocumentType.Name : "");
                else
                    spec.OrderByAscending(d => d.DocumentType != null ? d.DocumentType.Name : "");
                break;
            default:
                spec.OrderByDescending(d => d.Created);
                break;
        }
    }

    /// <summary>
    /// Parses the sort field from a DRF-style ordering parameter.
    /// Used for Lucene search sorting.
    /// </summary>
    private static string? ParseSortField(string? ordering)
    {
        if (string.IsNullOrWhiteSpace(ordering))
            return null;

        var fields = ordering.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (fields.Length == 0) return null;

        var firstField = fields[0];
        var fieldName = firstField.StartsWith('-') ? firstField[1..] : firstField;

        return fieldName.ToLowerInvariant() switch
        {
            "created" => "created",
            "added" => "added",
            "title" => "title",
            _ => null
        };
    }

    /// <summary>
    /// Determines if the ordering is descending.
    /// </summary>
    private static bool IsDescendingOrdering(string? ordering)
    {
        if (string.IsNullOrWhiteSpace(ordering))
            return true; // default is descending

        var fields = ordering.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (fields.Length == 0) return true;

        return fields[0].StartsWith('-');
    }

    /// <summary>
    /// Gets the storage path for a document's preview PDF.
    /// </summary>
    private static string GetPreviewPath(Document document)
    {
        // Preview path follows the pattern: previews/{id}.pdf
        return $"previews/{document.Id}.pdf";
    }

    /// <summary>
    /// Gets the storage path for a document's thumbnail PNG.
    /// </summary>
    private static string GetThumbnailPath(Document document)
    {
        // Thumbnail path follows the pattern: thumbnails/{id}.png
        return $"thumbnails/{document.Id}.png";
    }

    /// <summary>
    /// Determines the MIME content type based on file extension.
    /// </summary>
    private static string GetContentType(string filename)
    {
        var ext = Path.GetExtension(filename)?.ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".tiff" or ".tif" => "image/tiff",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }
}
