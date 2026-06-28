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

public class DocumentTypesControllerTests
{
    private readonly IDocumentTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DocumentTypesController> _logger;
    private readonly DocumentTypesController _controller;

    public DocumentTypesControllerTests()
    {
        _repository = Substitute.For<IDocumentTypeRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<DocumentTypesController>>();

        _controller = new DocumentTypesController(
            _repository,
            _unitOfWork,
            _logger);

        // Set up a default HttpContext for URL generation
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost:5001");
        httpContext.Request.Path = "/api/document_types";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private static DocumentType CreateTestDocumentType(int id = 1)
    {
        return new DocumentType
        {
            Id = id,
            Name = $"DocumentType {id}",
            Slug = $"document-type-{id}",
            Match = null,
            MatchingAlgorithm = Paperless.Core.Documents.Enums.MatchingAlgorithm.Auto,
            IsInsensitive = true,
            Documents = new List<Document>(),
            IsDeleted = false
        };
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/document_types/  —  List
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_WithoutFilters_ReturnsPaginatedResponse()
    {
        // Arrange
        var documentTypes = Enumerable.Range(1, 5)
            .Select(CreateTestDocumentType)
            .ToList()
            .AsReadOnly();

        _repository.GetAllAsync(Arg.Any<ISpecification<DocumentType>>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<DocumentType>
            {
                Items = documentTypes,
                TotalCount = 5,
                Page = 1,
                PageSize = 20
            });

        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(1, 5).Select(CreateTestDocumentType).ToList().AsReadOnly());

        // Act
        var filter = new DocumentTypeFilterRequest { Page = 1, PageSize = 20 };
        var result = await _controller.ListAsync(filter, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<DocumentTypeResponse>>(okResult.Value);
        Assert.Equal(5, response.Count);
        Assert.Equal(5, response.Results.Count);
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/document_types/  —  Create
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidRequest_Returns201()
    {
        // Arrange
        var request = new CreateDocumentTypeRequest
        {
            Name = "New Document Type"
        };

        _repository
            .AddAsync(default!, Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(callInfo =>
            {
                var dt = callInfo.ArgAt<DocumentType>(0);
                dt.Id = 42;
                return Task.FromResult(dt);
            });

        // Act
        var result = await _controller.CreateAsync(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        var response = Assert.IsType<DocumentTypeResponse>(createdResult.Value);
        Assert.Equal("New Document Type", response.Name);
        Assert.Equal(42, response.Id);
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/document_types/{id}/  —  Detail
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_WithExistingId_ReturnsDocumentType()
    {
        // Arrange
        var documentType = CreateTestDocumentType(1);
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(documentType);

        // Act
        var result = await _controller.GetAsync(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<DocumentTypeResponse>(okResult.Value);
        Assert.Equal(1, response.Id);
        Assert.Equal("DocumentType 1", response.Name);
    }

    [Fact]
    public async Task Get_WithNonexistentId_Returns404()
    {
        // Arrange
        _repository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((DocumentType?)null);

        // Act
        var result = await _controller.GetAsync(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_WithDeletedDocumentType_Returns404()
    {
        // Arrange
        var documentType = CreateTestDocumentType(1);
        documentType.IsDeleted = true;
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(documentType);

        // Act
        var result = await _controller.GetAsync(1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  PUT /api/document_types/{id}/  —  Full update
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithExistingDocumentType_ReturnsUpdated()
    {
        // Arrange
        var documentType = CreateTestDocumentType(1);
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(documentType);

        var request = new UpdateDocumentTypeRequest
        {
            Name = "Updated DocumentType",
            MatchingAlgorithm = 1
        };

        // Act
        var result = await _controller.UpdateAsync(1, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<DocumentTypeResponse>(okResult.Value);
        Assert.Equal("Updated DocumentType", response.Name);
    }

    [Fact]
    public async Task Update_WithNonexistentDocumentType_Returns404()
    {
        // Arrange
        _repository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((DocumentType?)null);

        // Act
        var result = await _controller.UpdateAsync(999, new UpdateDocumentTypeRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  PATCH /api/document_types/{id}/  —  Partial update
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Patch_WithPartialFields_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var documentType = CreateTestDocumentType(1);
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(documentType);

        var request = new PatchDocumentTypeRequest
        {
            Name = "Patched DocumentType"
        };

        // Act
        var result = await _controller.PatchAsync(1, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<DocumentTypeResponse>(okResult.Value);
        Assert.Equal("Patched DocumentType", response.Name);
    }

    [Fact]
    public async Task Patch_WithNonexistentDocumentType_Returns404()
    {
        // Arrange
        _repository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((DocumentType?)null);

        // Act
        var result = await _controller.PatchAsync(999, new PatchDocumentTypeRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  DELETE /api/document_types/{id}/  —  Soft delete
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithExistingDocumentType_Returns204()
    {
        // Arrange
        var documentType = CreateTestDocumentType(1);
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(documentType);

        // Act
        var result = await _controller.DeleteAsync(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _repository.Received(1).Delete(documentType);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WithNonexistentDocumentType_Returns404()
    {
        // Arrange
        _repository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((DocumentType?)null);

        // Act
        var result = await _controller.DeleteAsync(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
