using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paperless.Api.Dto.Requests;
using Paperless.Api.Dto.Responses;
using Paperless.Core.Common.Interfaces;
using Paperless.Core.Common.Specifications;
using Paperless.Core.Documents.Entities;

namespace Paperless.Api.Controllers;

/// <summary>
/// REST API controller for Tag CRUD operations.
/// Maps to the paperless-ngx TagViewSet.
/// All endpoints are under <c>/api/tags</c>.
/// </summary>
[ApiController]
[Route("/api/tags")]
[Authorize(Policy = "UserOrAdmin")]
[Produces("application/json")]
public class TagsController : ControllerBase
{
    private readonly ITagRepository _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TagsController> _logger;

    public TagsController(
        ITagRepository tagRepository,
        IUnitOfWork unitOfWork,
        ILogger<TagsController> logger)
    {
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/tags/  —  List tags
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paginated, filtered list of tags.
    /// Supports filtering by <c>is_inbox_tag</c>, searching by name, and ordering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<TagResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] TagFilterRequest filter,
        CancellationToken ct)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        // Build specification with combined filter conditions
        var spec = new Specification<Tag>();

        spec.Where(t =>
            !t.IsDeleted &&
            (!filter.IsInboxTag.HasValue || t.IsInboxTag == filter.IsInboxTag.Value) &&
            (string.IsNullOrWhiteSpace(filter.Query) ||
             t.Name.ToLower().Contains(filter.Query.ToLowerInvariant())));

        // Include the Documents navigation for document_count
        spec.Include(t => t.Documents);

        // Apply ordering
        ApplyOrdering(spec, filter.Ordering);

        // Apply pagination
        spec.ApplyPaging(page, pageSize);

        // Execute query
        var pagedResult = await _tagRepository.GetAllAsync(spec, ct);

        // Build all IDs list
        var allTags = await _tagRepository.GetAllAsync(ct);
        var filteredAll = allTags
            .Where(t => !t.IsDeleted)
            .Where(t => !filter.IsInboxTag.HasValue || t.IsInboxTag == filter.IsInboxTag.Value)
            .Where(t => string.IsNullOrWhiteSpace(filter.Query) ||
                        t.Name.Contains(filter.Query, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Id)
            .ToList()
            .AsReadOnly();

        var response = BuildPaginatedResponse(
            pagedResult.Items.Select(TagResponse.FromEntity).ToList().AsReadOnly(),
            pagedResult.TotalCount,
            page,
            pageSize,
            filteredAll);

        return Ok(response);
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/tags/  —  Create
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new tag.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateTagRequest request,
        CancellationToken ct)
    {
        var tag = new Tag
        {
            Name = request.Name,
            Slug = GenerateSlug(request.Name),
            Color = request.Color,
            TextColor = request.TextColor,
            Match = request.Match,
            MatchingAlgorithm = (Paperless.Core.Documents.Enums.MatchingAlgorithm)(request.MatchingAlgorithm ?? 6),
            IsInsensitive = request.IsInsensitive ?? true,
            IsInboxTag = request.IsInboxTag ?? false
        };

        var created = await _tagRepository.AddAsync(tag, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Created tag {TagId}: '{Name}'", created.Id, created.Name);

        var response = TagResponse.FromEntity(created);

        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, response);
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/tags/{id}/  —  Detail
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the full detail of a single tag by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        int id,
        CancellationToken ct)
    {
        var tag = await _tagRepository.GetByIdAsync(id, ct);

        if (tag is null || tag.IsDeleted)
        {
            return NotFound(new { detail = $"Tag with id {id} not found." });
        }

        return Ok(TagResponse.FromEntity(tag));
    }

    // ────────────────────────────────────────────────────────────────
    //  PUT /api/tags/{id}/  —  Full update
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fully replaces a tag's metadata.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAsync(
        int id,
        [FromBody] UpdateTagRequest request,
        CancellationToken ct)
    {
        var tag = await _tagRepository.GetByIdAsync(id, ct);

        if (tag is null || tag.IsDeleted)
        {
            return NotFound(new { detail = $"Tag with id {id} not found." });
        }

        // Apply full update
        tag.Name = request.Name;
        tag.Slug = GenerateSlug(request.Name);
        tag.Color = request.Color;
        tag.TextColor = request.TextColor;
        tag.Match = request.Match;
        tag.MatchingAlgorithm = (Paperless.Core.Documents.Enums.MatchingAlgorithm)(request.MatchingAlgorithm ?? 6);
        tag.IsInsensitive = request.IsInsensitive ?? true;
        tag.IsInboxTag = request.IsInboxTag ?? false;

        _tagRepository.Update(tag);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Updated tag {TagId}", tag.Id);

        return Ok(TagResponse.FromEntity(tag));
    }

    // ────────────────────────────────────────────────────────────────
    //  PATCH /api/tags/{id}/  —  Partial update
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Partially updates a tag's metadata. Only provided fields are changed.
    /// </summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchAsync(
        int id,
        [FromBody] PatchTagRequest request,
        CancellationToken ct)
    {
        var tag = await _tagRepository.GetByIdAsync(id, ct);

        if (tag is null || tag.IsDeleted)
        {
            return NotFound(new { detail = $"Tag with id {id} not found." });
        }

        // Apply partial update
        if (request.Name is not null)
        {
            tag.Name = request.Name;
            tag.Slug = GenerateSlug(request.Name);
        }

        if (request.Color is not null)
            tag.Color = request.Color;

        if (request.TextColor is not null)
            tag.TextColor = request.TextColor;

        if (request.Match is not null)
            tag.Match = request.Match;

        if (request.MatchingAlgorithm.HasValue)
            tag.MatchingAlgorithm = (Paperless.Core.Documents.Enums.MatchingAlgorithm)request.MatchingAlgorithm.Value;

        if (request.IsInsensitive.HasValue)
            tag.IsInsensitive = request.IsInsensitive.Value;

        if (request.IsInboxTag.HasValue)
            tag.IsInboxTag = request.IsInboxTag.Value;

        _tagRepository.Update(tag);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Patched tag {TagId}", tag.Id);

        return Ok(TagResponse.FromEntity(tag));
    }

    // ────────────────────────────────────────────────────────────────
    //  DELETE /api/tags/{id}/  —  Soft delete
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Soft-deletes a tag (marks as deleted).
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        int id,
        CancellationToken ct)
    {
        var tag = await _tagRepository.GetByIdAsync(id, ct);

        if (tag is null || tag.IsDeleted)
        {
            return NotFound(new { detail = $"Tag with id {id} not found." });
        }

        _tagRepository.Delete(tag);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Soft-deleted tag {TagId}", id);

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
    private static void ApplyOrdering(Specification<Tag> spec, string? ordering)
    {
        if (string.IsNullOrWhiteSpace(ordering))
        {
            spec.OrderByAscending(t => t.Name);
            return;
        }

        var fields = ordering.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (fields.Length == 0)
        {
            spec.OrderByAscending(t => t.Name);
            return;
        }

        var firstField = fields[0];
        var descending = firstField.StartsWith('-');
        var fieldName = descending ? firstField[1..] : firstField;

        switch (fieldName.ToLowerInvariant())
        {
            case "name":
                if (descending)
                    spec.OrderByDescending(t => t.Name);
                else
                    spec.OrderByAscending(t => t.Name);
                break;
            default:
                spec.OrderByAscending(t => t.Name);
                break;
        }
    }
}
