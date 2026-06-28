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

public class CorrespondentsControllerTests
{
    private readonly ICorrespondentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CorrespondentsController> _logger;
    private readonly CorrespondentsController _controller;

    public CorrespondentsControllerTests()
    {
        _repository = Substitute.For<ICorrespondentRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<CorrespondentsController>>();

        _controller = new CorrespondentsController(
            _repository,
            _unitOfWork,
            _logger);

        // Set up a default HttpContext for URL generation
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost:5001");
        httpContext.Request.Path = "/api/correspondents";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private static Correspondent CreateTestCorrespondent(int id = 1)
    {
        return new Correspondent
        {
            Id = id,
            Name = $"Correspondent {id}",
            Slug = $"correspondent-{id}",
            Match = null,
            MatchingAlgorithm = Paperless.Core.Documents.Enums.MatchingAlgorithm.Auto,
            IsInsensitive = true,
            Documents = new List<Document>(),
            IsDeleted = false
        };
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/correspondents/  —  List
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_WithoutFilters_ReturnsPaginatedResponse()
    {
        // Arrange
        var correspondents = Enumerable.Range(1, 5)
            .Select(CreateTestCorrespondent)
            .ToList()
            .AsReadOnly();

        _repository.GetAllAsync(Arg.Any<ISpecification<Correspondent>>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Correspondent>
            {
                Items = correspondents,
                TotalCount = 5,
                Page = 1,
                PageSize = 20
            });

        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(1, 5).Select(CreateTestCorrespondent).ToList().AsReadOnly());

        // Act
        var filter = new CorrespondentFilterRequest { Page = 1, PageSize = 20 };
        var result = await _controller.ListAsync(filter, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<CorrespondentResponse>>(okResult.Value);
        Assert.Equal(5, response.Count);
        Assert.Equal(5, response.Results.Count);
    }

    [Fact]
    public async Task List_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var allCorrespondents = Enumerable.Range(1, 10).Select(CreateTestCorrespondent).ToList();
        var page2Docs = allCorrespondents.Skip(5).Take(5).ToList().AsReadOnly();

        _repository.GetAllAsync(Arg.Any<ISpecification<Correspondent>>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Correspondent>
            {
                Items = page2Docs,
                TotalCount = 10,
                Page = 2,
                PageSize = 5
            });

        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(allCorrespondents.AsReadOnly());

        var filter = new CorrespondentFilterRequest { Page = 2, PageSize = 5 };

        // Act
        var result = await _controller.ListAsync(filter, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<CorrespondentResponse>>(okResult.Value);
        Assert.Equal(10, response.Count);
        Assert.Equal(5, response.Results.Count);
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/correspondents/  —  Create
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidRequest_Returns201()
    {
        // Arrange
        var request = new CreateCorrespondentRequest
        {
            Name = "New Correspondent"
        };

        _repository
            .AddAsync(default!, Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(callInfo =>
            {
                var c = callInfo.ArgAt<Correspondent>(0);
                c.Id = 42;
                return Task.FromResult(c);
            });

        // Act
        var result = await _controller.CreateAsync(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        var response = Assert.IsType<CorrespondentResponse>(createdResult.Value);
        Assert.Equal("New Correspondent", response.Name);
        Assert.Equal(42, response.Id);
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/correspondents/{id}/  —  Detail
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_WithExistingId_ReturnsCorrespondent()
    {
        // Arrange
        var correspondent = CreateTestCorrespondent(1);
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(correspondent);

        // Act
        var result = await _controller.GetAsync(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CorrespondentResponse>(okResult.Value);
        Assert.Equal(1, response.Id);
        Assert.Equal("Correspondent 1", response.Name);
    }

    [Fact]
    public async Task Get_WithNonexistentId_Returns404()
    {
        // Arrange
        _repository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Correspondent?)null);

        // Act
        var result = await _controller.GetAsync(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_WithDeletedCorrespondent_Returns404()
    {
        // Arrange
        var correspondent = CreateTestCorrespondent(1);
        correspondent.IsDeleted = true;
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(correspondent);

        // Act
        var result = await _controller.GetAsync(1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  PUT /api/correspondents/{id}/  —  Full update
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithExistingCorrespondent_ReturnsUpdated()
    {
        // Arrange
        var correspondent = CreateTestCorrespondent(1);
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(correspondent);

        var request = new UpdateCorrespondentRequest
        {
            Name = "Updated Name",
            MatchingAlgorithm = 1
        };

        // Act
        var result = await _controller.UpdateAsync(1, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CorrespondentResponse>(okResult.Value);
        Assert.Equal("Updated Name", response.Name);
    }

    [Fact]
    public async Task Update_WithNonexistentCorrespondent_Returns404()
    {
        // Arrange
        _repository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Correspondent?)null);

        // Act
        var result = await _controller.UpdateAsync(999, new UpdateCorrespondentRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  PATCH /api/correspondents/{id}/  —  Partial update
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Patch_WithPartialFields_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var correspondent = CreateTestCorrespondent(1);
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(correspondent);

        var request = new PatchCorrespondentRequest
        {
            Name = "Patched Name"
        };

        // Act
        var result = await _controller.PatchAsync(1, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CorrespondentResponse>(okResult.Value);
        Assert.Equal("Patched Name", response.Name);
    }

    [Fact]
    public async Task Patch_WithNonexistentCorrespondent_Returns404()
    {
        // Arrange
        _repository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Correspondent?)null);

        // Act
        var result = await _controller.PatchAsync(999, new PatchCorrespondentRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  DELETE /api/correspondents/{id}/  —  Soft delete
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithExistingCorrespondent_Returns204()
    {
        // Arrange
        var correspondent = CreateTestCorrespondent(1);
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(correspondent);

        // Act
        var result = await _controller.DeleteAsync(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _repository.Received(1).Delete(correspondent);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WithNonexistentCorrespondent_Returns404()
    {
        // Arrange
        _repository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Correspondent?)null);

        // Act
        var result = await _controller.DeleteAsync(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
