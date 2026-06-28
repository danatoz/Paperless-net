using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Paperless.Core.Common.Interfaces;
using Paperless.Infrastructure.FileStorage;

namespace Paperless.Infrastructure.Tests.FileStorage;

/// <summary>
/// Integration tests for <see cref="LocalFileStorage"/>.
/// Uses a temporary directory as the storage root for isolation.
/// </summary>
public class LocalFileStorageTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly LocalFileStorage _storage;
    private readonly FileStorageOptions _options;

    public LocalFileStorageTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"paperless-fs-test-{Guid.NewGuid():N}");
        _options = new FileStorageOptions
        {
            BaseDirectory = _tempRoot
        };
        _storage = new LocalFileStorage(Options.Create(_options));
    }

    [Fact]
    public async Task StoreAsync_Should_CreateFile_WithCorrectContent()
    {
        // Arrange
        var content = "Hello, Paperless!";
        var relativePath = "originals/test-document.pdf";

        // Act
        await _storage.StoreAsync(new MemoryStream(Encoding.UTF8.GetBytes(content)), relativePath);

        // Assert
        var fullPath = Path.Combine(_tempRoot, relativePath);
        File.Exists(fullPath).Should().BeTrue();
        var readContent = await File.ReadAllTextAsync(fullPath);
        readContent.Should().Be(content);
    }

    [Fact]
    public async Task StoreAsync_Should_CreateDirectories_Automatically()
    {
        // Arrange
        var content = "Nested directory test";
        var relativePath = "originals/2024/taxes/report.pdf";

        // Act
        await _storage.StoreAsync(new MemoryStream(Encoding.UTF8.GetBytes(content)), relativePath);

        // Assert
        var fullPath = Path.Combine(_tempRoot, relativePath);
        File.Exists(fullPath).Should().BeTrue();
    }

    [Fact]
    public async Task StoreAsync_Should_AtomicallyWrite_NoPartialFiles()
    {
        // Arrange
        var content = "Atomic write test";
        var relativePath = "originals/atomic-test.pdf";

        // Act
        await _storage.StoreAsync(new MemoryStream(Encoding.UTF8.GetBytes(content)), relativePath);

        // Assert
        // Verify there are no .tmp files left behind in the directory
        var directory = Path.Combine(_tempRoot, "originals");
        var tmpFiles = Directory.GetFiles(directory, "*.tmp.*");
        tmpFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task StoreAsync_Should_OverwriteExistingFile()
    {
        // Arrange
        var relativePath = "originals/overwrite-test.pdf";
        await _storage.StoreAsync(
            new MemoryStream(Encoding.UTF8.GetBytes("Original content")),
            relativePath);

        // Act
        await _storage.StoreAsync(
            new MemoryStream(Encoding.UTF8.GetBytes("Updated content")),
            relativePath);

        // Assert
        var fullPath = Path.Combine(_tempRoot, relativePath);
        var content = await File.ReadAllTextAsync(fullPath);
        content.Should().Be("Updated content");
    }

    [Fact]
    public async Task ReadAsync_Should_ReturnStoredContent()
    {
        // Arrange
        var content = "Read test content";
        var relativePath = "originals/read-test.pdf";
        await _storage.StoreAsync(new MemoryStream(Encoding.UTF8.GetBytes(content)), relativePath);

        // Act
        await using var stream = await _storage.ReadAsync(relativePath);
        using var reader = new StreamReader(stream);
        var readContent = await reader.ReadToEndAsync();

        // Assert
        readContent.Should().Be(content);
    }

    [Fact]
    public async Task ReadAsync_Should_ThrowFileNotFoundException_WhenFileDoesNotExist()
    {
        // Act
        var act = () => _storage.ReadAsync("originals/nonexistent.pdf");

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_Should_RemoveFile()
    {
        // Arrange
        var relativePath = "originals/to-delete.pdf";
        await _storage.StoreAsync(new MemoryStream(Encoding.UTF8.GetBytes("Delete me")), relativePath);

        // Act
        await _storage.DeleteAsync(relativePath);

        // Assert
        var fullPath = Path.Combine(_tempRoot, relativePath);
        File.Exists(fullPath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_Should_NotThrow_WhenFileDoesNotExist()
    {
        // Act
        var act = () => _storage.DeleteAsync("originals/never-existed.pdf");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MoveAsync_Should_MoveFileToNewLocation()
    {
        // Arrange
        var content = "Move test";
        var sourcePath = "originals/source.pdf";
        var destPath = "originals/destination.pdf";
        await _storage.StoreAsync(new MemoryStream(Encoding.UTF8.GetBytes(content)), sourcePath);

        // Act
        await _storage.MoveAsync(sourcePath, destPath);

        // Assert
        var fullSourcePath = Path.Combine(_tempRoot, sourcePath);
        var fullDestPath = Path.Combine(_tempRoot, destPath);
        File.Exists(fullSourcePath).Should().BeFalse();
        File.Exists(fullDestPath).Should().BeTrue();
        var destContent = await File.ReadAllTextAsync(fullDestPath);
        destContent.Should().Be(content);
    }

    [Fact]
    public async Task MoveAsync_Should_CreateDestinationDirectories()
    {
        // Arrange
        var content = "Move with directory creation";
        var sourcePath = "originals/source.pdf";
        var destPath = "archive/2024/moved-document.pdf";
        await _storage.StoreAsync(new MemoryStream(Encoding.UTF8.GetBytes(content)), sourcePath);

        // Act
        await _storage.MoveAsync(sourcePath, destPath);

        // Assert
        var fullDestPath = Path.Combine(_tempRoot, destPath);
        File.Exists(fullDestPath).Should().BeTrue();
    }

    [Fact]
    public async Task MoveAsync_Should_OverwriteExistingDestination()
    {
        // Arrange
        var sourcePath = "originals/source-overwrite.pdf";
        var destPath = "originals/dest-overwrite.pdf";
        await _storage.StoreAsync(new MemoryStream(Encoding.UTF8.GetBytes("Source content")), sourcePath);
        await _storage.StoreAsync(new MemoryStream(Encoding.UTF8.GetBytes("Dest content")), destPath);

        // Act
        await _storage.MoveAsync(sourcePath, destPath);

        // Assert
        var fullDestPath = Path.Combine(_tempRoot, destPath);
        var content = await File.ReadAllTextAsync(fullDestPath);
        content.Should().Be("Source content");
    }

    [Fact]
    public async Task MoveAsync_Should_ThrowFileNotFoundException_WhenSourceDoesNotExist()
    {
        // Act
        var act = () => _storage.MoveAsync("originals/nonexistent-source.pdf", "originals/dest.pdf");

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task GetThumbnailAsync_Should_ReadThumbnailFile()
    {
        // Arrange
        var content = "Thumbnail PNG data";
        var relativePath = "thumbnails/0000001.png";
        await _storage.StoreAsync(new MemoryStream(Encoding.UTF8.GetBytes(content)), relativePath);

        // Act
        await using var stream = await _storage.GetThumbnailAsync(relativePath);
        using var reader = new StreamReader(stream);
        var readContent = await reader.ReadToEndAsync();

        // Assert
        readContent.Should().Be(content);
    }

    [Fact]
    public async Task GetPreviewAsync_Should_ReadPreviewFile()
    {
        // Arrange
        var content = "Preview PDF data";
        var relativePath = "previews/0000001.pdf";
        await _storage.StoreAsync(new MemoryStream(Encoding.UTF8.GetBytes(content)), relativePath);

        // Act
        await using var stream = await _storage.GetPreviewAsync(relativePath);
        using var reader = new StreamReader(stream);
        var readContent = await reader.ReadToEndAsync();

        // Assert
        readContent.Should().Be(content);
    }

    [Fact]
    public async Task StoreAsync_Should_ThrowArgumentNullException_WhenStreamIsNull()
    {
        // Act
        var act = () => _storage.StoreAsync(null!, "originals/test.pdf");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StoreAsync_Should_ThrowArgumentException_WhenPathIsEmpty()
    {
        // Act
        var act = () => _storage.StoreAsync(new MemoryStream(), "");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    public void Dispose()
    {
        // Clean up the temporary test directory
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }

        GC.SuppressFinalize(this);
    }
}
