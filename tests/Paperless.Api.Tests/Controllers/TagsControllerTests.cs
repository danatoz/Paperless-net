using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Paperless.Api.Controllers;
using Paperless.Api.Dto.Requests;
using Paperless.Api.Dto.Responses;
using Paperless.Core.Common.Interfaces;
using Paperless.Core.Common.Models;
using Paperless.Core.Common.Specifications;
using Paperless.Core.Documents.Entities;

namespace Paperless.Api.Tests.Controllers;

public class TagsControllerTests
{
    private readonly ITagRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TagsController> _logger;
    private readonly TagsController _controller;

    public TagsControllerTests()
    {
        _repository = Substitute.For<ITagRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<TagsController>>();

        _controller = new TagsController(
            _repository,
            _unitOfWork,
            _logger);

        // Set up a default HttpContext for URL generation
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost:5001");
        httpContext.Request.Path = "/api/tags";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private static Tag CreateTestTag(int id = 1)
    {
        return new Tag
        {
            Id = id,
            Name = $"Tag {id}",
            Slug = $"tag-{id}",
            Color = "#FF5733",
            TextColor = "#FFFFFF",
            Match = null,
            MatchingAlgorithm = Paperless.Core.Documents.Enums.MatchingAlgorithm.Auto,
            IsInsensitive = true,
            IsInboxTag = false,
            Documents = new List<Document>(),
            IsDeleted = false
        };
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/tags/  —  List
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_WithoutFilters_ReturnsPaginatedResponse()
    {
        // Arrange
        var tags = Enumerable.Range(1, 5)
            .Select(CreateTestTag)
            .ToList()
            .AsReadOnly();

        _repository.GetAllAsync(Arg.Any<ISpecification<Tag>>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Tag>
            {
                Items = tags,
                TotalCount = 5,
                Page = 1,
                PageSize = 20
            });

        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(1, 5).Select(CreateTestTag).ToList().AsReadOnly());

        // Act
        var filter = new TagFilterRequest { Page = 1, PageSize = 20 };
        var result = await _controller.ListAsync(filter, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<TagResponse>>(okResult.Value);
        Assert.Equal(5, response.Count);
        Assert.Equal(5, response.Results.Count);
    }

    [Fact]
    public async Task List_WithInboxTagFilter_ReturnsFilteredTags()
    {
        // Arrange
        var inboxTags = new[] { CreateTestTag(2) };
        inboxTags[0].IsInboxTag = true;

        _repository.GetAllAsync(Arg.Any<ISpecification<Tag>>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Tag>
            {
                Items = inboxTags.ToList().AsReadOnly(),
                TotalCount = 1,
                Page = 1,
                PageSize = 20
            });

        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(1, 5).Select(CreateTestTag).ToList().AsReadOnly());

        // Act
        var filter = new TagFilterRequest { IsInboxTag = true };
        var result = await _controller.ListAsync(filter, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<TagResponse>>(okResult.Value);
        Assert.Equal(1, response.Count);
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/tags/  —  Create
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidRequest_Returns201()
    {
        // Arrange
        var request = new CreateTagRequest
        {
            Name = "New Tag",
            Color = "#00FF00",
            IsInboxTag = true
        };

        _repository
            .AddAsync(default!, Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(callInfo =>
            {
                var tag = callInfo.ArgAt<Tag>(0);
                tag.Id = 42;
                return Task.FromResult(tag);
            });

        // Act
        var result = await _controller.CreateAsync(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        var response = Assert.IsType<TagResponse>(createdResult.Value);
        Assert.Equal("New Tag", response.Name);
        Assert.Equal("#00FF00", response.Color);
        Assert.True(response.IsInboxTag);
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/tags/{id}/  —  Detail
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_WithExistingId_ReturnsTag()
    {
        // Arrange
        var tag = CreateTestTag(1);
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(tag);

        // Act
        var result = await _controller.GetAsync(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TagResponse>(okResult.Value);
        Assert.Equal(1, response.Id);
        Assert.Equal("Tag 1", response.Name);
    }

    [Fact]
    public async Task Get_WithNonexistentId_Returns404()
    {
        // Arrange
        _repository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Tag?)null);

        // Act
        var result = await _controller.GetAsync(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_WithDeletedTag_Returns404()
    {
        // Arrange
        var tag = CreateTestTag(1);
        tag.IsDeleted = true;
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(tag);

        // Act
        var result = await _controller.GetAsync(1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  PUT /api/tags/{id}/  —  Full update
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithExistingTag_ReturnsUpdated()
    {
        // Arrange
        var tag = CreateTestTag(1);
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(tag);

        var request = new UpdateTagRequest
        {
            Name = "Updated Tag",
            Color = "#0000FF",
            IsInboxTag = true
        };

        // Act
        var result = await _controller.UpdateAsync(1, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TagResponse>(okResult.Value);
        Assert.Equal("Updated Tag", response.Name);
        Assert.Equal("#0000FF", response.Color);
    }

    [Fact]
    public async Task Update_WithNonexistentTag_Returns404()
    {
        // Arrange
        _repository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Tag?)null);

        // Act
        var result = await _controller.UpdateAsync(999, new UpdateTagRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  PATCH /api/tags/{id}/  —  Partial update
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Patch_WithPartialFields_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var tag = CreateTestTag(1);
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(tag);

        var request = new PatchTagRequest
        {
            Name = "Patched Tag"
        };

        // Act
        var result = await _controller.PatchAsync(1, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TagResponse>(okResult.Value);
        Assert.Equal("Patched Tag", response.Name);
    }

    [Fact]
    public async Task Patch_WithNonexistentTag_Returns404()
    {
        // Arrange
        _repository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Tag?)null);

        // Act
        var result = await _controller.PatchAsync(999, new PatchTagRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  DELETE /api/tags/{id}/  —  Soft delete
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithExistingTag_Returns204()
    {
        // Arrange
        var tag = CreateTestTag(1);
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(tag);

        // Act
        var result = await _controller.DeleteAsync(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _repository.Received(1).Delete(tag);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WithNonexistentTag_Returns404()
    {
        // Arrange
        _repository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Tag?)null);

        // Act
        var result = await _controller.DeleteAsync(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
