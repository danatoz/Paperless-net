using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paperless.Api.Dto.Requests;
using Paperless.Api.Dto.Responses;
using Paperless.Core.Common.Interfaces;
using Paperless.Core.Common.Specifications;
using Paperless.Core.Documents.Entities;

namespace Paperless.Api.Controllers;

/// <summary>
/// REST API controller for Correspondent CRUD operations.
/// Maps to the paperless-ngx CorrespondentViewSet.
/// All endpoints are under <c>/api/correspondents</c>.
/// </summary>
[ApiController]
[Route("/api/correspondents")]
[Authorize(Policy = "UserOrAdmin")]
[Produces("application/json")]
public class CorrespondentsController : ControllerBase
{
    private readonly ICorrespondentRepository _correspondentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CorrespondentsController> _logger;

    public CorrespondentsController(
        ICorrespondentRepository correspondentRepository,
        IUnitOfWork unitOfWork,
        ILogger<CorrespondentsController> logger)
    {
        _correspondentRepository = correspondentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/correspondents/  —  List correspondents
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paginated, filtered list of correspondents.
    /// Supports searching by name and ordering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<CorrespondentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] CorrespondentFilterRequest filter,
        CancellationToken ct)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        // Build specification with combined filter conditions
        var spec = new Specification<Correspondent>();

        // Combine all filter conditions into a single Where call
        spec.Where(c =>
            !c.IsDeleted &&
            (string.IsNullOrWhiteSpace(filter.Query) ||
             c.Name.ToLower().Contains(filter.Query.ToLowerInvariant())));

        // Include the Documents navigation for document_count
        spec.Include(c => c.Documents);

        // Apply ordering
        ApplyOrdering(spec, filter.Ordering);

        // Apply pagination
        spec.ApplyPaging(page, pageSize);

        // Execute query
        var pagedResult = await _correspondentRepository.GetAllAsync(spec, ct);

        // Build all IDs list (for the "all" field in DRF-format pagination)
        var allSpec = new Specification<Correspondent>();
        allSpec.Where(c =>
            !c.IsDeleted &&
            (string.IsNullOrWhiteSpace(filter.Query) ||
             c.Name.ToLower().Contains(filter.Query.ToLowerInvariant())));
        var allItems = await _correspondentRepository.GetAllAsync(ct);
        var filteredAll = allItems
            .Where(c => !c.IsDeleted)
            .Where(c => string.IsNullOrWhiteSpace(filter.Query) ||
                        c.Name.Contains(filter.Query, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Id)
            .ToList()
            .AsReadOnly();

        var response = BuildPaginatedResponse(
            pagedResult.Items.Select(CorrespondentResponse.FromEntity).ToList().AsReadOnly(),
            pagedResult.TotalCount,
            page,
            pageSize,
            filteredAll);

        return Ok(response);
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/correspondents/  —  Create
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new correspondent.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CorrespondentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateCorrespondentRequest request,
        CancellationToken ct)
    {
        var correspondent = new Correspondent
        {
            Name = request.Name,
            Slug = GenerateSlug(request.Name),
            Match = request.Match,
            MatchingAlgorithm = (Paperless.Core.Documents.Enums.MatchingAlgorithm)(request.MatchingAlgorithm ?? 6),
            IsInsensitive = request.IsInsensitive ?? true
        };

        var created = await _correspondentRepository.AddAsync(correspondent, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Created correspondent {CorrespondentId}: '{Name}'", created.Id, created.Name);

        var response = CorrespondentResponse.FromEntity(created);

        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, response);
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/correspondents/{id}/  —  Detail
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the full detail of a single correspondent by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CorrespondentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        int id,
        CancellationToken ct)
    {
        var correspondent = await _correspondentRepository.GetByIdAsync(id, ct);

        if (correspondent is null || correspondent.IsDeleted)
        {
            return NotFound(new { detail = $"Correspondent with id {id} not found." });
        }

        return Ok(CorrespondentResponse.FromEntity(correspondent));
    }

    // ────────────────────────────────────────────────────────────────
    //  PUT /api/correspondents/{id}/  —  Full update
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fully replaces a correspondent's metadata.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CorrespondentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAsync(
        int id,
        [FromBody] UpdateCorrespondentRequest request,
        CancellationToken ct)
    {
        var correspondent = await _correspondentRepository.GetByIdAsync(id, ct);

        if (correspondent is null || correspondent.IsDeleted)
        {
            return NotFound(new { detail = $"Correspondent with id {id} not found." });
        }

        // Apply full update
        correspondent.Name = request.Name;
        correspondent.Slug = GenerateSlug(request.Name);
        correspondent.Match = request.Match;
        correspondent.MatchingAlgorithm = (Paperless.Core.Documents.Enums.MatchingAlgorithm)(request.MatchingAlgorithm ?? 6);
        correspondent.IsInsensitive = request.IsInsensitive ?? true;

        _correspondentRepository.Update(correspondent);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Updated correspondent {CorrespondentId}", correspondent.Id);

        return Ok(CorrespondentResponse.FromEntity(correspondent));
    }

    // ────────────────────────────────────────────────────────────────
    //  PATCH /api/correspondents/{id}/  —  Partial update
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Partially updates a correspondent's metadata. Only provided fields are changed.
    /// </summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(CorrespondentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchAsync(
        int id,
        [FromBody] PatchCorrespondentRequest request,
        CancellationToken ct)
    {
        var correspondent = await _correspondentRepository.GetByIdAsync(id, ct);

        if (correspondent is null || correspondent.IsDeleted)
        {
            return NotFound(new { detail = $"Correspondent with id {id} not found." });
        }

        // Apply partial update (only non-null fields)
        if (request.Name is not null)
        {
            correspondent.Name = request.Name;
            correspondent.Slug = GenerateSlug(request.Name);
        }

        if (request.Match is not null)
            correspondent.Match = request.Match;

        if (request.MatchingAlgorithm.HasValue)
            correspondent.MatchingAlgorithm = (Paperless.Core.Documents.Enums.MatchingAlgorithm)request.MatchingAlgorithm.Value;

        if (request.IsInsensitive.HasValue)
            correspondent.IsInsensitive = request.IsInsensitive.Value;

        _correspondentRepository.Update(correspondent);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Patched correspondent {CorrespondentId}", correspondent.Id);

        return Ok(CorrespondentResponse.FromEntity(correspondent));
    }

    // ────────────────────────────────────────────────────────────────
    //  DELETE /api/correspondents/{id}/  —  Soft delete
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Soft-deletes a correspondent (marks as deleted).
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        int id,
        CancellationToken ct)
    {
        var correspondent = await _correspondentRepository.GetByIdAsync(id, ct);

        if (correspondent is null || correspondent.IsDeleted)
        {
            return NotFound(new { detail = $"Correspondent with id {id} not found." });
        }

        _correspondentRepository.Delete(correspondent);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Soft-deleted correspondent {CorrespondentId}", id);

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
    private static void ApplyOrdering(Specification<Correspondent> spec, string? ordering)
    {
        if (string.IsNullOrWhiteSpace(ordering))
        {
            spec.OrderByAscending(c => c.Name);
            return;
        }

        var fields = ordering.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (fields.Length == 0)
        {
            spec.OrderByAscending(c => c.Name);
            return;
        }

        var firstField = fields[0];
        var descending = firstField.StartsWith('-');
        var fieldName = descending ? firstField[1..] : firstField;

        switch (fieldName.ToLowerInvariant())
        {
            case "name":
                if (descending)
                    spec.OrderByDescending(c => c.Name);
                else
                    spec.OrderByAscending(c => c.Name);
                break;
            default:
                spec.OrderByAscending(c => c.Name);
                break;
        }
    }
}
