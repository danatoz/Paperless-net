using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paperless.Api.Dto.Requests;
using Paperless.Api.Dto.Responses;
using Paperless.Core.Common.Interfaces;
using Paperless.Core.Common.Specifications;
using Paperless.Core.Documents.Entities;

namespace Paperless.Api.Controllers;

/// <summary>
/// REST API controller for DocumentType CRUD operations.
/// Maps to the paperless-ngx DocumentTypeViewSet.
/// All endpoints are under <c>/api/document_types</c>.
/// </summary>
[ApiController]
[Route("/api/document_types")]
[Authorize(Policy = "UserOrAdmin")]
[Produces("application/json")]
public class DocumentTypesController : ControllerBase
{
    private readonly IDocumentTypeRepository _documentTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DocumentTypesController> _logger;

    public DocumentTypesController(
        IDocumentTypeRepository documentTypeRepository,
        IUnitOfWork unitOfWork,
        ILogger<DocumentTypesController> logger)
    {
        _documentTypeRepository = documentTypeRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/document_types/  —  List document types
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paginated, filtered list of document types.
    /// Supports searching by name and ordering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<DocumentTypeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] DocumentTypeFilterRequest filter,
        CancellationToken ct)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        // Build specification with combined filter conditions
        var spec = new Specification<DocumentType>();

        spec.Where(dt =>
            !dt.IsDeleted &&
            (string.IsNullOrWhiteSpace(filter.Query) ||
             dt.Name.ToLower().Contains(filter.Query.ToLowerInvariant())));

        // Include the Documents navigation for document_count
        spec.Include(dt => dt.Documents);

        // Apply ordering
        ApplyOrdering(spec, filter.Ordering);

        // Apply pagination
        spec.ApplyPaging(page, pageSize);

        // Execute query
        var pagedResult = await _documentTypeRepository.GetAllAsync(spec, ct);

        // Build all IDs list
        var allItems = await _documentTypeRepository.GetAllAsync(ct);
        var filteredAll = allItems
            .Where(dt => !dt.IsDeleted)
            .Where(dt => string.IsNullOrWhiteSpace(filter.Query) ||
                         dt.Name.Contains(filter.Query, StringComparison.OrdinalIgnoreCase))
            .Select(dt => dt.Id)
            .ToList()
            .AsReadOnly();

        var response = BuildPaginatedResponse(
            pagedResult.Items.Select(DocumentTypeResponse.FromEntity).ToList().AsReadOnly(),
            pagedResult.TotalCount,
            page,
            pageSize,
            filteredAll);

        return Ok(response);
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/document_types/  —  Create
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new document type.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentTypeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateDocumentTypeRequest request,
        CancellationToken ct)
    {
        var documentType = new DocumentType
        {
            Name = request.Name,
            Slug = GenerateSlug(request.Name),
            Match = request.Match,
            MatchingAlgorithm = (Paperless.Core.Documents.Enums.MatchingAlgorithm)(request.MatchingAlgorithm ?? 6),
            IsInsensitive = request.IsInsensitive ?? true
        };

        var created = await _documentTypeRepository.AddAsync(documentType, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Created document type {DocumentTypeId}: '{Name}'", created.Id, created.Name);

        var response = DocumentTypeResponse.FromEntity(created);

        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, response);
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/document_types/{id}/  —  Detail
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the full detail of a single document type by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DocumentTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        int id,
        CancellationToken ct)
    {
        var documentType = await _documentTypeRepository.GetByIdAsync(id, ct);

        if (documentType is null || documentType.IsDeleted)
        {
            return NotFound(new { detail = $"Document type with id {id} not found." });
        }

        return Ok(DocumentTypeResponse.FromEntity(documentType));
    }

    // ────────────────────────────────────────────────────────────────
    //  PUT /api/document_types/{id}/  —  Full update
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fully replaces a document type's metadata.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(DocumentTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAsync(
        int id,
        [FromBody] UpdateDocumentTypeRequest request,
        CancellationToken ct)
    {
        var documentType = await _documentTypeRepository.GetByIdAsync(id, ct);

        if (documentType is null || documentType.IsDeleted)
        {
            return NotFound(new { detail = $"Document type with id {id} not found." });
        }

        // Apply full update
        documentType.Name = request.Name;
        documentType.Slug = GenerateSlug(request.Name);
        documentType.Match = request.Match;
        documentType.MatchingAlgorithm = (Paperless.Core.Documents.Enums.MatchingAlgorithm)(request.MatchingAlgorithm ?? 6);
        documentType.IsInsensitive = request.IsInsensitive ?? true;

        _documentTypeRepository.Update(documentType);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Updated document type {DocumentTypeId}", documentType.Id);

        return Ok(DocumentTypeResponse.FromEntity(documentType));
    }

    // ────────────────────────────────────────────────────────────────
    //  PATCH /api/document_types/{id}/  —  Partial update
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Partially updates a document type's metadata. Only provided fields are changed.
    /// </summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(DocumentTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchAsync(
        int id,
        [FromBody] PatchDocumentTypeRequest request,
        CancellationToken ct)
    {
        var documentType = await _documentTypeRepository.GetByIdAsync(id, ct);

        if (documentType is null || documentType.IsDeleted)
        {
            return NotFound(new { detail = $"Document type with id {id} not found." });
        }

        // Apply partial update (only non-null fields)
        if (request.Name is not null)
        {
            documentType.Name = request.Name;
            documentType.Slug = GenerateSlug(request.Name);
        }

        if (request.Match is not null)
            documentType.Match = request.Match;

        if (request.MatchingAlgorithm.HasValue)
            documentType.MatchingAlgorithm = (Paperless.Core.Documents.Enums.MatchingAlgorithm)request.MatchingAlgorithm.Value;

        if (request.IsInsensitive.HasValue)
            documentType.IsInsensitive = request.IsInsensitive.Value;

        _documentTypeRepository.Update(documentType);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Patched document type {DocumentTypeId}", documentType.Id);

        return Ok(DocumentTypeResponse.FromEntity(documentType));
    }

    // ────────────────────────────────────────────────────────────────
    //  DELETE /api/document_types/{id}/  —  Soft delete
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Soft-deletes a document type (marks as deleted).
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        int id,
        CancellationToken ct)
    {
        var documentType = await _documentTypeRepository.GetByIdAsync(id, ct);

        if (documentType is null || documentType.IsDeleted)
        {
            return NotFound(new { detail = $"Document type with id {id} not found." });
        }

        _documentTypeRepository.Delete(documentType);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Soft-deleted document type {DocumentTypeId}", id);

        return NoContent();
    }

    // ────────────────────────────────────────────────────────────────
    //  Private helpers
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a URL-safe slug from a name.
    /// </summary>
    private static string? GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(".", "-")
            .Replace("_", "-")
            .Trim('-');
    }

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
            var path = request.Path.Value ?? "";

            var query = System.Web.HttpUtility.ParseQueryString(request.QueryString.Value ?? "");
            query["page"] = targetPage.ToString();
            query["page_size"] = pageSize.ToString();

            return $"{scheme}://{host}{path}?{query}";
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
    /// Applies ordering to a specification.
    /// </summary>
    private static void ApplyOrdering(Specification<DocumentType> spec, string? ordering)
    {
        if (string.IsNullOrWhiteSpace(ordering))
        {
            spec.OrderByAscending(dt => dt.Name);
            return;
        }

        var fields = ordering.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (fields.Length == 0)
        {
            spec.OrderByAscending(dt => dt.Name);
            return;
        }

        var firstField = fields[0];
        var descending = firstField.StartsWith('-');
        var fieldName = descending ? firstField[1..] : firstField;

        switch (fieldName.ToLowerInvariant())
        {
            case "name":
                if (descending)
                    spec.OrderByDescending(dt => dt.Name);
                else
                    spec.OrderByAscending(dt => dt.Name);
                break;
            default:
                spec.OrderByAscending(dt => dt.Name);
                break;
        }
    }
}
