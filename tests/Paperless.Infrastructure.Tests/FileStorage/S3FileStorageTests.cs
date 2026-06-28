using System.Net;
using System.Text;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Paperless.Core.Common.Interfaces;
using Paperless.Infrastructure.FileStorage;

namespace Paperless.Infrastructure.Tests.FileStorage;

/// <summary>
/// Unit tests for <see cref="S3FileStorage"/>.
/// Uses NSubstitute to mock <see cref="IAmazonS3"/> so no real S3
/// connection is required.
/// </summary>
public class S3FileStorageTests
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3FileStorage _storage;
    private readonly S3StorageOptions _options;

    public S3FileStorageTests()
    {
        _s3Client = Substitute.For<IAmazonS3>();
        _options = new S3StorageOptions
        {
            BucketName = "test-bucket",
            Region = "us-east-1",
        };
        _storage = new S3FileStorage(
            _s3Client,
            _options,
            NullLogger<S3FileStorage>.Instance);
    }

    [Fact]
    public async Task StoreAsync_Should_CallPutObjectAsync_WithCorrectParameters()
    {
        // Arrange
        var content = "S3 test content";
        var path = "originals/test-document.pdf";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        PutObjectRequest? capturedRequest = null;
        await _s3Client.PutObjectAsync(
            Arg.Do<PutObjectRequest>(req => capturedRequest = req),
            Arg.Any<CancellationToken>());

        // Act
        await _storage.StoreAsync(stream, path);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.BucketName.Should().Be(_options.BucketName);
        capturedRequest.Key.Should().Be(path);
        capturedRequest.InputStream.Should().BeSameAs(stream);
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

    [Fact]
    public async Task ReadAsync_Should_ReturnStream_WhenFileExists()
    {
        // Arrange
        var path = "originals/existing-file.pdf";
        var expectedContent = "Existing file content";
        var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes(expectedContent));

        _s3Client.GetObjectAsync(
                Arg.Is<GetObjectRequest>(r =>
                    r.BucketName == _options.BucketName && r.Key == path),
                Arg.Any<CancellationToken>())
            .Returns(new GetObjectResponse
            {
                BucketName = _options.BucketName,
                Key = path,
                ResponseStream = expectedStream,
                HttpStatusCode = HttpStatusCode.OK,
            });

        // Act
        await using var resultStream = await _storage.ReadAsync(path);
        using var reader = new StreamReader(resultStream);
        var content = await reader.ReadToEndAsync();

        // Assert
        content.Should().Be(expectedContent);
    }

    [Fact]
    public async Task ReadAsync_Should_ThrowFileNotFoundException_WhenFileDoesNotExist()
    {
        // Arrange
        var path = "originals/nonexistent.pdf";

        _s3Client.GetObjectAsync(
                Arg.Is<GetObjectRequest>(r =>
                    r.BucketName == _options.BucketName && r.Key == path),
                Arg.Any<CancellationToken>())
            .Throws(new AmazonS3Exception("Not found")
            {
                StatusCode = HttpStatusCode.NotFound,
            });

        // Act
        var act = () => _storage.ReadAsync(path);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ReadAsync_Should_ThrowArgumentException_WhenPathIsEmpty()
    {
        // Act
        var act = () => _storage.ReadAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeleteAsync_Should_CallDeleteObjectAsync_WithCorrectParameters()
    {
        // Arrange
        var path = "originals/to-delete.pdf";

        DeleteObjectRequest? capturedRequest = null;
        _s3Client.DeleteObjectAsync(
                Arg.Do<DeleteObjectRequest>(req => capturedRequest = req),
                Arg.Any<CancellationToken>())
            .Returns(new DeleteObjectResponse());

        // Act
        await _storage.DeleteAsync(path);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.BucketName.Should().Be(_options.BucketName);
        capturedRequest.Key.Should().Be(path);
    }

    [Fact]
    public async Task DeleteAsync_Should_NotThrow_WhenFileDoesNotExist()
    {
        // Arrange
        var path = "originals/never-existed.pdf";

        _s3Client.DeleteObjectAsync(
                Arg.Any<DeleteObjectRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new DeleteObjectResponse());

        // Act
        var act = () => _storage.DeleteAsync(path);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MoveAsync_Should_CallCopyObject_ThenDeleteObject()
    {
        // Arrange
        var sourcePath = "originals/source.pdf";
        var destPath = "archive/destination.pdf";

        CopyObjectRequest? capturedCopyRequest = null;
        DeleteObjectRequest? capturedDeleteRequest = null;

        _s3Client.CopyObjectAsync(
                Arg.Do<CopyObjectRequest>(req => capturedCopyRequest = req),
                Arg.Any<CancellationToken>())
            .Returns(new CopyObjectResponse());

        _s3Client.DeleteObjectAsync(
                Arg.Do<DeleteObjectRequest>(req => capturedDeleteRequest = req),
                Arg.Any<CancellationToken>())
            .Returns(new DeleteObjectResponse());

        // Act
        await _storage.MoveAsync(sourcePath, destPath);

        // Assert
        capturedCopyRequest.Should().NotBeNull();
        capturedCopyRequest!.SourceBucket.Should().Be(_options.BucketName);
        capturedCopyRequest.SourceKey.Should().Be(sourcePath);
        capturedCopyRequest.DestinationBucket.Should().Be(_options.BucketName);
        capturedCopyRequest.DestinationKey.Should().Be(destPath);

        capturedDeleteRequest.Should().NotBeNull();
        capturedDeleteRequest!.BucketName.Should().Be(_options.BucketName);
        capturedDeleteRequest.Key.Should().Be(sourcePath);
    }

    [Fact]
    public async Task MoveAsync_Should_ThrowFileNotFoundException_WhenSourceDoesNotExist()
    {
        // Arrange
        var sourcePath = "originals/nonexistent-source.pdf";
        var destPath = "archive/dest.pdf";

        _s3Client.CopyObjectAsync(
                Arg.Any<CopyObjectRequest>(),
                Arg.Any<CancellationToken>())
            .Throws(new AmazonS3Exception("Not found")
            {
                StatusCode = HttpStatusCode.NotFound,
            });

        // Act
        var act = () => _storage.MoveAsync(sourcePath, destPath);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task MoveAsync_Should_ThrowArgumentException_WhenSourcePathIsEmpty()
    {
        // Act
        var act = () => _storage.MoveAsync("", "dest.pdf");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task MoveAsync_Should_ThrowArgumentException_WhenDestPathIsEmpty()
    {
        // Act
        var act = () => _storage.MoveAsync("source.pdf", "");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetThumbnailAsync_Should_DelegateToReadAsync()
    {
        // Arrange
        var path = "thumbnails/0000001.png";
        var expectedContent = "Thumbnail PNG data";
        var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes(expectedContent));

        _s3Client.GetObjectAsync(
                Arg.Is<GetObjectRequest>(r =>
                    r.BucketName == _options.BucketName && r.Key == path),
                Arg.Any<CancellationToken>())
            .Returns(new GetObjectResponse
            {
                BucketName = _options.BucketName,
                Key = path,
                ResponseStream = expectedStream,
                HttpStatusCode = HttpStatusCode.OK,
            });

        // Act
        await using var resultStream = await _storage.GetThumbnailAsync(path);
        using var reader = new StreamReader(resultStream);
        var content = await reader.ReadToEndAsync();

        // Assert
        content.Should().Be(expectedContent);
    }

    [Fact]
    public async Task GetPreviewAsync_Should_DelegateToReadAsync()
    {
        // Arrange
        var path = "previews/0000001.pdf";
        var expectedContent = "Preview PDF data";
        var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes(expectedContent));

        _s3Client.GetObjectAsync(
                Arg.Is<GetObjectRequest>(r =>
                    r.BucketName == _options.BucketName && r.Key == path),
                Arg.Any<CancellationToken>())
            .Returns(new GetObjectResponse
            {
                BucketName = _options.BucketName,
                Key = path,
                ResponseStream = expectedStream,
                HttpStatusCode = HttpStatusCode.OK,
            });

        // Act
        await using var resultStream = await _storage.GetPreviewAsync(path);
        using var reader = new StreamReader(resultStream);
        var content = await reader.ReadToEndAsync();

        // Assert
        content.Should().Be(expectedContent);
    }

    [Fact]
    public void Constructor_Should_ThrowInvalidOperationException_WhenBucketNameIsEmpty()
    {
        // Arrange
        var options = new S3StorageOptions
        {
            BucketName = "",
        };

        // Act
        var act = () => new S3FileStorage(
            _s3Client,
            options,
            NullLogger<S3FileStorage>.Instance);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*bucket name*");
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_WhenS3ClientIsNull()
    {
        // Act
        var act = () => new S3FileStorage(
            null!,
            _options,
            NullLogger<S3FileStorage>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_WhenOptionsIsNull()
    {
        // Act
        var act = () => new S3FileStorage(
            _s3Client,
            null!,
            NullLogger<S3FileStorage>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task StoreAsync_Should_NormalizeBackslashPaths()
    {
        // Arrange
        var content = "Path with Windows separators";
        var windowsPath = @"originals\subdir\test.pdf";
        var expectedKey = "originals/subdir/test.pdf";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        PutObjectRequest? capturedRequest = null;
        await _s3Client.PutObjectAsync(
            Arg.Do<PutObjectRequest>(req => capturedRequest = req),
            Arg.Any<CancellationToken>());

        // Act
        await _storage.StoreAsync(stream, windowsPath);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Key.Should().Be(expectedKey);
    }

    [Fact]
    public async Task MoveAsync_Should_NormalizePaths()
    {
        // Arrange
        var sourcePath = @"originals\source.pdf";
        var destPath = @"archive\dest.pdf";
        var expectedSourceKey = "originals/source.pdf";
        var expectedDestKey = "archive/dest.pdf";

        CopyObjectRequest? capturedRequest = null;
        _s3Client.CopyObjectAsync(
                Arg.Do<CopyObjectRequest>(req => capturedRequest = req),
                Arg.Any<CancellationToken>())
            .Returns(new CopyObjectResponse());

        _s3Client.DeleteObjectAsync(
                Arg.Any<DeleteObjectRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new DeleteObjectResponse());

        // Act
        await _storage.MoveAsync(sourcePath, destPath);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.SourceKey.Should().Be(expectedSourceKey);
        capturedRequest.DestinationKey.Should().Be(expectedDestKey);
    }
}
