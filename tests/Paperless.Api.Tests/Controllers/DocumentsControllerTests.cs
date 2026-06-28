using System.Text;
using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Paperless.Api.Controllers;
using Paperless.Api.Dto.Requests;
using Paperless.Api.Dto.Responses;
using Paperless.Api.Jobs;
using Paperless.Core.Common.Interfaces;
using Paperless.Core.Common.Models;
using Paperless.Core.Common.Specifications;
using Paperless.Core.Documents.Entities;
using Paperless.Shared.Abstractions;
using Document = Paperless.Core.Documents.Entities.Document;

namespace Paperless.Api.Tests.Controllers;

public class DocumentsControllerTests
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly ISearchBackend _searchBackend;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<DocumentsController> _logger;
    private readonly DocumentsController _controller;

    public DocumentsControllerTests()
    {
        _documentRepository = Substitute.For<IDocumentRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _fileStorage = Substitute.For<IFileStorage>();
        _searchBackend = Substitute.For<ISearchBackend>();
        _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
        _logger = Substitute.For<ILogger<DocumentsController>>();

        _controller = new DocumentsController(
            _documentRepository,
            _unitOfWork,
            _fileStorage,
            _searchBackend,
            _backgroundJobClient,
            _logger);

        // Set up a default HttpContext for URL generation
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost:5001");
        httpContext.Request.Path = "/api/documents";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    // ────────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────────

    private static Document CreateTestDocument(int id = 1)
    {
        return new Document
        {
            Id = id,
            Title = $"Test Document {id}",
            Content = $"Content of document {id}",
            Created = DateTime.UtcNow.AddDays(-id),
            Added = DateTime.UtcNow,
            Modified = DateTime.UtcNow,
            CorrespondentId = 1,
            DocumentTypeId = 1,
            Filename = $"{id}/original.pdf",
            Checksum = $"checksum{id}",
            ArchiveSerialNumber = id * 100,
            Tags = new List<Tag>
            {
                new() { Id = 1, Name = "Tag1" },
                new() { Id = 2, Name = "Tag2" }
            },
            CustomFields = new List<DocumentCustomField>(),
            OwnerId = 1,
            IsDeleted = false
        };
    }

    private static int GetDocumentIdFromRoute(ControllerContext ctx)
    {
        var idStr = ctx.RouteData.Values["id"] as string;
        return int.TryParse(idStr, out var id) ? id : 0;
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/documents/  —  List documents
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_WithoutFilters_ReturnsPaginatedResponse()
    {
        // Arrange — 25 items, page_size=10 => page 1 has next, no previous
        var documents = Enumerable.Range(1, 10) // first page of 10
            .Select(CreateTestDocument)
            .ToList()
            .AsReadOnly();

        _documentRepository.GetAllAsync(Arg.Any<ISpecification<Document>>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Document>
            {
                Items = documents,
                TotalCount = 25,
                Page = 1,
                PageSize = 10
            });

        // Act
        var filter = new DocumentFilterRequest { Page = 1, PageSize = 10 };
        var result = await _controller.ListAsync(filter, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<DocumentResponse>>(okResult.Value);
        Assert.Equal(25, response.Count);
        Assert.Equal(10, response.Results.Count);
        Assert.NotNull(response.Next);
        Assert.Contains("page=2", response.Next);
        Assert.Null(response.Previous);
    }

    [Fact]
    public async Task List_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var allDocs = Enumerable.Range(1, 25).Select(CreateTestDocument).ToList();
        var page2Docs = allDocs.Skip(20).Take(5).ToList().AsReadOnly();

        _documentRepository.GetAllAsync(Arg.Any<ISpecification<Document>>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Document>
            {
                Items = page2Docs,
                TotalCount = 25,
                Page = 2,
                PageSize = 5
            });

        var filter = new DocumentFilterRequest { Page = 2, PageSize = 5 };

        // Act
        var result = await _controller.ListAsync(filter, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<DocumentResponse>>(okResult.Value);
        Assert.Equal(25, response.Count);
        Assert.Equal(5, response.Results.Count);
    }

    [Fact]
    public async Task List_WithSearchQuery_DelegatesToSearchBackend()
    {
        // Arrange
        _searchBackend.SearchAsync("test query", Arg.Any<CancellationToken>())
            .Returns(new[] { 1, 2, 3 });

        var documents = Enumerable.Range(1, 3)
            .Select(CreateTestDocument)
            .ToList()
            .AsReadOnly();

        _documentRepository.GetAllAsync(Arg.Any<ISpecification<Document>>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Document>
            {
                Items = documents,
                TotalCount = 3,
                Page = 1,
                PageSize = 20
            });

        var filter = new DocumentFilterRequest { Query = "test query" };

        // Act
        var result = await _controller.ListAsync(filter, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<DocumentResponse>>(okResult.Value);
        Assert.Equal(3, response.Count);
        await _searchBackend.Received(1).SearchAsync("test query", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_WithEmptySearchResult_ReturnsEmptyPage()
    {
        // Arrange
        _searchBackend.SearchAsync("nonexistent", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<int>());

        var filter = new DocumentFilterRequest { Query = "nonexistent" };

        // Act
        var result = await _controller.ListAsync(filter, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<DocumentResponse>>(okResult.Value);
        Assert.Equal(0, response.Count);
        Assert.Empty(response.Results);
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/documents/  —  Create
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidRequest_Returns201WithDocument()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            Title = "New Document",
            Content = "Some content",
            CorrespondentId = 1,
            TagIds = new[] { 1, 2 }
        };

        Document? capturedDoc = null;
        _documentRepository
            .AddAsync(default!, Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(callInfo =>
            {
                capturedDoc = callInfo.ArgAt<Document>(0);
                capturedDoc.Id = 42;
                return Task.FromResult(capturedDoc);
            });

        // Act
        var result = await _controller.CreateAsync(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        var response = Assert.IsType<DocumentResponse>(createdResult.Value);
        Assert.Equal("New Document", response.Title);
        Assert.Equal(42, response.Id);
    }

    [Fact]
    public async Task Create_WithoutTitle_UsesDefault()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            Content = "Content only"
        };

        _documentRepository
            .AddAsync(Arg.Is<Document>(d => d.Title == "Untitled"), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new Document
            {
                Id = 1,
                Title = "Untitled",
                Content = "Content only"
            }));

        // Act
        var result = await _controller.CreateAsync(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<DocumentResponse>(createdResult.Value);
        Assert.Equal("Untitled", response.Title);
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/documents/{id}/  —  Detail
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_WithExistingId_ReturnsDocument()
    {
        // Arrange
        var doc = CreateTestDocument(1);
        _documentRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(doc);

        // Act
        var result = await _controller.GetAsync(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<DocumentResponse>(okResult.Value);
        Assert.Equal(1, response.Id);
        Assert.Equal("Test Document 1", response.Title);
    }

    [Fact]
    public async Task Get_WithNonexistentId_Returns404()
    {
        // Arrange
        _documentRepository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Document?)null);

        // Act
        var result = await _controller.GetAsync(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_WithDeletedDocument_Returns404()
    {
        // Arrange
        var doc = CreateTestDocument(1);
        doc.IsDeleted = true;
        _documentRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(doc);

        // Act
        var result = await _controller.GetAsync(1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  PUT /api/documents/{id}/  —  Full update
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithExistingDocument_ReturnsUpdated()
    {
        // Arrange
        var doc = CreateTestDocument(1);
        _documentRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(doc);

        var request = new UpdateDocumentRequest
        {
            Title = "Updated Title",
            Content = "Updated content",
            TagIds = new[] { 3, 4 }
        };

        // Act
        var result = await _controller.UpdateAsync(1, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<DocumentResponse>(okResult.Value);
        Assert.Equal("Updated Title", response.Title);
    }

    [Fact]
    public async Task Update_WithNonexistentDocument_Returns404()
    {
        // Arrange
        _documentRepository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Document?)null);

        // Act
        var result = await _controller.UpdateAsync(999, new UpdateDocumentRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  PATCH /api/documents/{id}/  —  Partial update
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Patch_WithPartialFields_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var doc = CreateTestDocument(1);
        _documentRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(doc);

        var request = new PatchDocumentRequest
        {
            Title = "Patched Title"
            // Only title is provided, other fields should not be changed
        };

        // Act
        var result = await _controller.PatchAsync(1, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<DocumentResponse>(okResult.Value);
        Assert.Equal("Patched Title", response.Title);
    }

    [Fact]
    public async Task Patch_WithNonexistentDocument_Returns404()
    {
        // Arrange
        _documentRepository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Document?)null);

        // Act
        var result = await _controller.PatchAsync(999, new PatchDocumentRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  DELETE /api/documents/{id}/  —  Soft delete
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithExistingDocument_Returns204()
    {
        // Arrange
        var doc = CreateTestDocument(1);
        _documentRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(doc);

        // Act
        var result = await _controller.DeleteAsync(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _documentRepository.Received(1).Delete(doc);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WithNonexistentDocument_Returns404()
    {
        // Arrange
        _documentRepository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Document?)null);

        // Act
        var result = await _controller.DeleteAsync(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/documents/{id}/download/  —  Download
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Download_WithExistingDocument_ReturnsFile()
    {
        // Arrange
        var doc = CreateTestDocument(1);
        doc.Filename = "1/original.pdf";
        _documentRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(doc);

        var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake pdf content"));
        _fileStorage.ReadAsync("1/original.pdf", Arg.Any<CancellationToken>()).Returns(stream);

        // Act
        var result = await _controller.DownloadAsync(1, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
    }

    [Fact]
    public async Task Download_WhenFileNotFound_Returns404()
    {
        // Arrange
        var doc = CreateTestDocument(1);
        doc.Filename = "1/original.pdf";
        _documentRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(doc);
        _fileStorage.ReadAsync("1/original.pdf", Arg.Any<CancellationToken>())
            .Returns<Stream>(_ => throw new FileNotFoundException());

        // Act
        var result = await _controller.DownloadAsync(1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/documents/{id}/preview/  —  Preview
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Preview_WithExistingDocument_ReturnsPdfFile()
    {
        // Arrange
        var doc = CreateTestDocument(1);
        _documentRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(doc);

        var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake pdf preview"));
        _fileStorage.GetPreviewAsync("previews/1.pdf", Arg.Any<CancellationToken>()).Returns(stream);

        // Act
        var result = await _controller.PreviewAsync(1, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/documents/{id}/thumb/  —  Thumbnail
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Thumbnail_WithExistingDocument_ReturnsPngFile()
    {
        // Arrange
        var doc = CreateTestDocument(1);
        _documentRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(doc);

        var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake png thumbnail"));
        _fileStorage.GetThumbnailAsync("thumbnails/1.png", Arg.Any<CancellationToken>()).Returns(stream);

        // Act
        var result = await _controller.ThumbnailAsync(1, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/documents/bulk_edit/  —  Bulk edit
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BulkEdit_WithValidRequest_Returns202()
    {
        // Arrange
        var request = new BulkEditRequest
        {
            DocumentIds = new[] { 1, 2, 3 },
            Method = "set_correspondent",
            Parameters = new Dictionary<string, object?> { ["correspondent"] = 5 }
        };

        // Enqueue is an extension method, so we mock the underlying Create call.
        _backgroundJobClient
            .Create(Arg.Any<Hangfire.Common.Job>(), Arg.Any<Hangfire.States.IState>())
            .Returns("job-123");

        // Act
        var result = _controller.BulkEdit(request);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        Assert.Equal(202, acceptedResult.StatusCode);
    }

    [Fact]
    public async Task BulkEdit_WithEmptyDocumentIds_Returns400()
    {
        // Arrange
        var request = new BulkEditRequest
        {
            DocumentIds = Array.Empty<int>(),
            Method = "set_correspondent"
        };

        // Act
        var result = _controller.BulkEdit(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task BulkEdit_WithoutMethod_Returns400()
    {
        // Arrange
        var request = new BulkEditRequest
        {
            DocumentIds = new[] { 1, 2 },
            Method = ""
        };

        // Act
        var result = _controller.BulkEdit(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/documents/delete/  —  Bulk delete
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BulkDelete_WithValidRequest_Returns202()
    {
        // Arrange
        var request = new DocumentSetRequest { DocumentIds = new[] { 1, 2 } };

        // Act
        var result = _controller.BulkDelete(request);

        // Assert
        Assert.IsType<AcceptedResult>(result);
    }

    [Fact]
    public async Task BulkDelete_WithEmptyIds_Returns400()
    {
        // Arrange
        var request = new DocumentSetRequest { DocumentIds = Array.Empty<int>() };

        // Act
        var result = _controller.BulkDelete(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/documents/reprocess/  —  Reprocess
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reprocess_WithValidRequest_Returns202()
    {
        // Arrange
        var request = new DocumentSetRequest { DocumentIds = new[] { 1, 2, 3 } };

        // Act
        var result = _controller.Reprocess(request);

        // Assert
        Assert.IsType<AcceptedResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/documents/rotate/  —  Rotate
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Rotate_WithValidRequest_Returns202()
    {
        // Arrange
        var request = new RotateRequest { DocumentIds = new[] { 1, 2 }, Rotation = 90 };

        // Act
        var result = _controller.Rotate(request);

        // Assert
        Assert.IsType<AcceptedResult>(result);
    }

    [Fact]
    public async Task Rotate_WithInvalidRotation_Returns400()
    {
        // Arrange
        var request = new RotateRequest { DocumentIds = new[] { 1, 2 }, Rotation = 45 };

        // Act
        var result = _controller.Rotate(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/documents/merge/  —  Merge
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Merge_WithTwoOrMoreDocuments_Returns202()
    {
        // Arrange
        var request = new MergeRequest { DocumentIds = new[] { 1, 2, 3 } };

        // Act
        var result = _controller.Merge(request);

        // Assert
        Assert.IsType<AcceptedResult>(result);
    }

    [Fact]
    public async Task Merge_WithSingleDocument_Returns400()
    {
        // Arrange
        var request = new MergeRequest { DocumentIds = new[] { 1 } };

        // Act
        var result = _controller.Merge(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/documents/edit_pdf/  —  Edit PDF
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EditPdf_WithValidRequest_Returns202()
    {
        // Act
        var result = _controller.EditPdf(null, "1,2,3");

        // Assert
        Assert.IsType<AcceptedResult>(result);
    }

    [Fact]
    public async Task EditPdf_WithEmptyDocuments_Returns400()
    {
        // Act
        var result = _controller.EditPdf(null, "");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/documents/remove_password/  —  Remove password
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RemovePassword_WithValidRequest_Returns202()
    {
        // Arrange
        var request = new RemovePasswordRequest
        {
            DocumentIds = new[] { 1, 2 },
            Password = "secret"
        };

        // Act
        var result = _controller.RemovePassword(request);

        // Assert
        Assert.IsType<AcceptedResult>(result);
    }

    [Fact]
    public async Task RemovePassword_WithoutPassword_Returns400()
    {
        // Arrange
        var request = new RemovePasswordRequest
        {
            DocumentIds = new[] { 1, 2 },
            Password = ""
        };

        // Act
        var result = _controller.RemovePassword(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /api/documents/bulk_download/  —  Bulk download
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BulkDownload_WithValidRequest_Returns202()
    {
        // Arrange
        var request = new BulkDownloadRequest
        {
            DocumentIds = new[] { 1, 2, 3 },
            Content = "both"
        };

        // Act
        var result = _controller.BulkDownload(request);

        // Assert
        Assert.IsType<AcceptedResult>(result);
    }

    [Fact]
    public async Task BulkDownload_WithEmptyIds_Returns400()
    {
        // Arrange
        var request = new BulkDownloadRequest { DocumentIds = Array.Empty<int>() };

        // Act
        var result = _controller.BulkDownload(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  GET /api/documents/selection_data/  —  Selection data
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SelectionData_ReturnsSelectionInfo()
    {
        // Arrange
        var doc1 = CreateTestDocument(1);
        var doc2 = CreateTestDocument(2);
        var documents = new[] { doc1, doc2 }.ToList().AsReadOnly();

        _documentRepository.GetAllAsync(Arg.Any<ISpecification<Document>>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Document>
            {
                Items = documents,
                TotalCount = 2
            });

        // Act
        var result = await _controller.SelectionData(null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SelectionDataResponse>(okResult.Value);
        Assert.Equal(2, response.All.DocumentCount);
        Assert.NotEmpty(response.Selected.DocumentIds);
    }
}
