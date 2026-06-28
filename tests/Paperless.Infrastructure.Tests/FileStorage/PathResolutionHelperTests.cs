using FluentAssertions;
using Paperless.Core.Documents.Entities;
using Paperless.Infrastructure.FileStorage;

namespace Paperless.Infrastructure.Tests.FileStorage;

/// <summary>
/// Unit tests for <see cref="PathResolutionHelper"/>.
/// Tests path computation logic independent of any actual file I/O.
/// </summary>
public class PathResolutionHelperTests
{
    [Fact]
    public void GetOriginalFilePath_Should_ReturnPathWithOriginalsSubdirectory()
    {
        // Arrange
        var doc = new Document
        {
            Id = 1,
            Filename = "invoice.pdf"
        };

        // Act
        var path = PathResolutionHelper.GetOriginalFilePath(doc);

        // Assert
        path.Should().Be("originals/invoice.pdf");
    }

    [Fact]
    public void GetOriginalFilePath_Should_UseStoragePath_WhenAvailable()
    {
        // Arrange
        var doc = new Document
        {
            Id = 1,
            Filename = "invoice.pdf",
            StoragePath = "2024/taxes/q1-report.pdf"
        };

        // Act
        var path = PathResolutionHelper.GetOriginalFilePath(doc);

        // Assert
        path.Should().Be("originals/2024/taxes/q1-report.pdf");
    }

    [Fact]
    public void GetOriginalFilePath_Should_FallbackToIdBasedPath_WhenNoFilename()
    {
        // Arrange
        var doc = new Document
        {
            Id = 42,
            Filename = null
        };

        // Act
        var path = PathResolutionHelper.GetOriginalFilePath(doc);

        // Assert
        path.Should().Be("originals/0000042.pdf");
    }

    [Fact]
    public void GetOriginalFilePath_Should_PreserveFileExtension_FromFilename()
    {
        // Arrange
        var doc = new Document
        {
            Id = 7,
            Filename = "report.pdf"
        };

        // Act
        var path = PathResolutionHelper.GetOriginalFilePath(doc);

        // Assert
        path.Should().Be("originals/report.pdf");
    }

    [Fact]
    public void GetArchiveFilePath_Should_ReturnPathWithArchiveSubdirectory()
    {
        // Arrange
        var doc = new Document
        {
            Id = 1,
            Filename = "invoice.pdf"
        };

        // Act
        var path = PathResolutionHelper.GetArchiveFilePath(doc);

        // Assert
        path.Should().Be("archive/invoice.pdf");
    }

    [Fact]
    public void GetArchiveFilePath_Should_UseStemFromOriginalFilename()
    {
        // Arrange
        var doc = new Document
        {
            Id = 1,
            Filename = "taxes/2024/q1-report.pdf"
        };

        // Act
        var path = PathResolutionHelper.GetArchiveFilePath(doc);

        // Assert
        path.Should().Be("archive/q1-report.pdf");
    }

    [Fact]
    public void GetArchiveFilePath_Should_FallbackToIdBasedPath_WhenNoFilename()
    {
        // Arrange
        var doc = new Document
        {
            Id = 99,
            Filename = null
        };

        // Act
        var path = PathResolutionHelper.GetArchiveFilePath(doc);

        // Assert
        path.Should().Be("archive/0000099.pdf");
    }

    [Fact]
    public void GetThumbnailPath_Should_ReturnPngPathInThumbnailsSubdirectory()
    {
        // Arrange
        var doc = new Document
        {
            Id = 123,
            Filename = "doc.pdf"
        };

        // Act
        var path = PathResolutionHelper.GetThumbnailPath(doc);

        // Assert
        path.Should().Be("thumbnails/0000123.png");
    }

    [Fact]
    public void GetPreviewPath_Should_ReturnPdfPathInPreviewsSubdirectory()
    {
        // Arrange
        var doc = new Document
        {
            Id = 456,
            Filename = "doc.pdf"
        };

        // Act
        var path = PathResolutionHelper.GetPreviewPath(doc);

        // Assert
        path.Should().Be("previews/0000456.pdf");
    }

    [Fact]
    public void GetSubdirectoryFromPath_Should_ExtractTopLevelSubdirectory()
    {
        // Act
        var subdir = PathResolutionHelper.GetSubdirectoryFromPath("originals/2024/taxes/doc.pdf");

        // Assert
        subdir.Should().Be("originals");
    }

    [Fact]
    public void GetSubdirectoryFromPath_Should_ReturnFullPath_WhenNoSeparator()
    {
        // Act
        var subdir = PathResolutionHelper.GetSubdirectoryFromPath("justafile.pdf");

        // Assert
        subdir.Should().Be("justafile.pdf");
    }

    [Fact]
    public void GetOriginalFilePath_Should_ThrowArgumentNullException_WhenDocumentIsNull()
    {
        // Act
        var act = () => PathResolutionHelper.GetOriginalFilePath(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetArchiveFilePath_Should_ThrowArgumentNullException_WhenDocumentIsNull()
    {
        // Act
        var act = () => PathResolutionHelper.GetArchiveFilePath(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetOriginalFilePath_Should_PadDocumentId_WithLeadingZeros()
    {
        // Arrange
        var doc = new Document
        {
            Id = 7,
            Filename = null,
            StoragePath = null
        };

        // Act
        var path = PathResolutionHelper.GetOriginalFilePath(doc);

        // Assert
        path.Should().Be("originals/0000007.pdf");
    }
}
