using Microsoft.AspNetCore.Http;
using Moq;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Infrastructure.Services;
using System.Text;
using File = System.IO.File;

namespace TravelHub.Tests.Infrastructure.Tests.Services
{
    public class PhotoServiceTests : IDisposable
    {
        private readonly Mock<IPhotoRepository> _photoRepositoryMock;
        private readonly PhotoService _photoService;
        private readonly string _tempTestDirectory;

        public PhotoServiceTests()
        {
            _photoRepositoryMock = new Mock<IPhotoRepository>();
            _photoService = new PhotoService(_photoRepositoryMock.Object);

            // Create temp directory for tests
            _tempTestDirectory = Path.Combine(Path.GetTempPath(), "PhotoServiceTests");
            Directory.CreateDirectory(_tempTestDirectory);
        }

        public void Dispose()
        {
            // Clean up temp directory after tests
            if (Directory.Exists(_tempTestDirectory))
            {
                Directory.Delete(_tempTestDirectory, true);
            }
        }

        #region GetBySpotIdAsync Tests

        [Fact]
        public async Task GetBySpotIdAsync_WithExistingSpotId_ReturnsPhotos()
        {
            // Arrange
            var spotId = 1;
            var expectedPhotos = new List<Photo>
            {
                new() { Id = 1, SpotId = spotId, FilePath = "test", Name = "test" },
                new() {Id = 2, SpotId = spotId, FilePath = "test", Name = "test"}
            };

            _photoRepositoryMock
                .Setup(x => x.GetPhotosBySpotIdAsync(spotId))
                .ReturnsAsync(expectedPhotos);

            // Act
            var result = await _photoService.GetBySpotIdAsync(spotId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.Equal(spotId, p.SpotId));
        }

        [Fact]
        public async Task GetBySpotIdAsync_WithNonExistingSpotId_ReturnsEmptyList()
        {
            // Arrange
            var spotId = 999;

            _photoRepositoryMock
                .Setup(x => x.GetPhotosBySpotIdAsync(spotId))
                .ReturnsAsync(new List<Photo>());

            // Act
            var result = await _photoService.GetBySpotIdAsync(spotId);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region AddSpotPhotoAsync Tests

        [Fact]
        public async Task AddSpotPhotoAsync_WithValidData_AddsPhoto()
        {
            // Arrange
            var photo = new Photo { Alt = "Test Alt", FilePath = "test", Name = "test" };
            var fileStream = new MemoryStream(new byte[1000]);
            var fileName = "test.jpg";
            var webRootPath = _tempTestDirectory;

            var expectedPhoto = new Photo { Id = 1, Name = "generated.jpg", FilePath = "/images/spots/generated.jpg" };
            _photoRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Photo>()))
                .ReturnsAsync((Photo p) =>
                {
                    p.Id = 1;
                    return p;
                });

            // Act
            var result = await _photoService.AddSpotPhotoAsync(photo, fileStream, fileName, webRootPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.StartsWith("/images/spots/", result.FilePath);
            Assert.EndsWith(".jpg", result.FilePath);
            Assert.Equal("Test Alt", result.Alt);

            _photoRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Photo>()), Times.Once);
        }

        [Fact]
        public async Task AddSpotPhotoAsync_WithNullStream_ThrowsArgumentNullException()
        {
            // Arrange
            var photo = new Photo { FilePath = "test", Name = "test" };
            Stream fileStream = null;
            var fileName = "test.jpg";
            var webRootPath = _tempTestDirectory;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _photoService.AddSpotPhotoAsync(photo, fileStream, fileName, webRootPath));
        }

        [Fact]
        public async Task AddSpotPhotoAsync_WithEmptyFileName_ThrowsArgumentException()
        {
            // Arrange
            var photo = new Photo { FilePath = "test", Name = "test" };
            var fileStream = new MemoryStream();
            var fileName = "";
            var webRootPath = _tempTestDirectory;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _photoService.AddSpotPhotoAsync(photo, fileStream, fileName, webRootPath));
        }

        [Fact]
        public async Task AddSpotPhotoAsync_WithInvalidExtension_ThrowsArgumentException()
        {
            // Arrange
            var photo = new Photo{ FilePath = "test", Name = "test" };
            var fileStream = new MemoryStream();
            var fileName = "test.txt";
            var webRootPath = _tempTestDirectory;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _photoService.AddSpotPhotoAsync(photo, fileStream, fileName, webRootPath));
        }

        [Fact]
        public async Task AddSpotPhotoAsync_WithTooLargeFile_ThrowsArgumentException()
        {
            // Arrange
            var photo = new Photo{ FilePath = "test", Name = "test" };
            var fileStream = new MemoryStream(new byte[11 * 1024 * 1024]); // 11 MB
            var fileName = "test.jpg";
            var webRootPath = _tempTestDirectory;

            // Act & Assert
            await Assert.ThrowsAsync < ArgumentException>(() =>
                _photoService.AddSpotPhotoAsync(photo, fileStream, fileName, webRootPath));
        }

        [Fact]
        public async Task AddSpotPhotoAsync_WithNullAlt_SetsDefaultAlt()
        {
            // Arrange
            var photo = new Photo{ FilePath = "test", Name = "test" }; // Alt is null
            var fileStream = new MemoryStream(new byte[1000]);
            var fileName = "test.jpg";
            var webRootPath = _tempTestDirectory;

            _photoRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Photo>()))
                .ReturnsAsync((Photo p) => p);

            // Act
            var result = await _photoService.AddSpotPhotoAsync(photo, fileStream, fileName, webRootPath);

            // Assert
            Assert.Equal("no alternative image description", result.Alt);
        }

        [Fact]
        public async Task AddSpotPhotoAsync_WithNonSeekableStream_SkipsSizeCheck()
        {
            // Arrange
            var photo = new Photo{ FilePath = "test", Name = "test" };
            var fileStream = new NonSeekableStream(new byte[1000]);
            var fileName = "test.jpg";
            var webRootPath = _tempTestDirectory;

            _photoRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Photo>()))
                .ReturnsAsync((Photo p) => p);

            // Act & Assert - Should not throw
            var result = await _photoService.AddSpotPhotoAsync(photo, fileStream, fileName, webRootPath);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region DeleteSpotPhotoAsync Tests

        [Fact]
        public async Task DeleteSpotPhotoAsync_WithExistingPhoto_DeletesPhotoAndFile()
        {
            // Arrange
            var photoId = 1;
            var webRootPath = _tempTestDirectory;

            // Create test file
            var testFilePath = Path.Combine(webRootPath, "images", "spots", "test.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(testFilePath));
            await File.WriteAllBytesAsync(testFilePath, new byte[100]);

            var photo = new Photo
            {
                Id = photoId,
                FilePath = "/images/spots/test.jpg",
                Name = "test"
            };

            _photoRepositoryMock
                .Setup(x => x.GetByIdAsync(photoId))
                .ReturnsAsync(photo);
            _photoRepositoryMock
                .Setup(x => x.DeleteAsync(photo))
                .Returns(Task.CompletedTask);

            // Act
            await _photoService.DeleteSpotPhotoAsync(photoId, webRootPath);

            // Assert
            _photoRepositoryMock.Verify(x => x.DeleteAsync(photo), Times.Once);
            Assert.False(File.Exists(testFilePath)); // File should be deleted
        }

        [Fact]
        public async Task DeleteSpotPhotoAsync_WithNonExistingPhoto_DoesNothing()
        {
            // Arrange
            var photoId = 999;
            var webRootPath = _tempTestDirectory;

            _photoRepositoryMock
                .Setup(x => x.GetByIdAsync(photoId))
                .ReturnsAsync((Photo)null);

            // Act
            await _photoService.DeleteSpotPhotoAsync(photoId, webRootPath);

            // Assert
            _photoRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Photo>()), Times.Never);
        }

        [Fact]
        public async Task DeleteSpotPhotoAsync_WithNonExistingFile_StillDeletesPhoto()
        {
            // Arrange
            var photoId = 1;
            var webRootPath = _tempTestDirectory;
            var photo = new Photo
            {
                Id = photoId,
                FilePath = "/images/spots/nonexistent.jpg", // File doesn't exist
                Name = "test"
            };

            _photoRepositoryMock
                .Setup(x => x.GetByIdAsync(photoId))
                .ReturnsAsync(photo);
            _photoRepositoryMock
                .Setup(x => x.DeleteAsync(photo))
                .Returns(Task.CompletedTask);

            // Act
            await _photoService.DeleteSpotPhotoAsync(photoId, webRootPath);

            // Assert
            _photoRepositoryMock.Verify(x => x.DeleteAsync(photo), Times.Once);
        }

        #endregion

        #region GetByPostIdAsync Tests

        [Fact]
        public async Task GetByPostIdAsync_WithExistingPostId_ReturnsPhotos()
        {
            // Arrange
            var postId = 1;
            var expectedPhotos = new List<Photo>
            {
                new() { Id = 1, PostId = postId, FilePath = "test", Name = "test" },
                new() { Id = 2, PostId = postId, FilePath = "test", Name = "test" }
            };

            _photoRepositoryMock
                .Setup(x => x.GetPhotosByPostIdAsync(postId))
                .ReturnsAsync(expectedPhotos);

            // Act
            var result = await _photoService.GetByPostIdAsync(postId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.Equal(postId, p.PostId));
        }

        #endregion

        #region GetByCommentIdAsync Tests

        [Fact]
        public async Task GetByCommentIdAsync_WithExistingCommentId_ReturnsPhotos()
        {
            // Arrange
            var commentId = 1;
            var expectedPhotos = new List<Photo>
            {
                new() { Id = 1, CommentId = commentId, FilePath = "test", Name = "test" },
                new() { Id = 2, CommentId = commentId, FilePath = "test", Name = "test" }
            };

            _photoRepositoryMock
                .Setup(x => x.GetByCommentIdAsync(commentId))
                .ReturnsAsync(expectedPhotos);

            // Act
            var result = await _photoService.GetByCommentIdAsync(commentId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.Equal(commentId, p.CommentId));
        }

        #endregion

        #region AddBlogPhotoAsync Tests

        [Fact]
        public async Task AddBlogPhotoAsync_WithValidFile_AddsPhoto()
        {
            // Arrange
            var photoFile = CreateMockFormFile("test.jpg", "Hello World from a Fake File");
            var webRootPath = _tempTestDirectory;
            var subFolder = "blog";
            var postId = 1;
            var commentId = 1;
            var altText = "Test Alt";

            _photoRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Photo>()))
                .ReturnsAsync((Photo p) =>
                {
                    p.Id = 1;
                    return p;
                });

            // Act
            var result = await _photoService.AddBlogPhotoAsync(photoFile.Object, webRootPath, subFolder, postId, commentId, altText);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.StartsWith($"/images/{subFolder}/", result.FilePath);
            Assert.Contains("test.jpg", result.FilePath);
            Assert.Equal("Test Alt", result.Alt);
            Assert.Equal(postId, result.PostId);
            Assert.Equal(commentId, result.CommentId);

            _photoRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Photo>()), Times.Once);
        }

        [Fact]
        public async Task AddBlogPhotoAsync_WithNullFile_ThrowsArgumentException()
        {
            // Arrange
            IFormFile photoFile = null;
            var webRootPath = _tempTestDirectory;
            var subFolder = "blog";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _photoService.AddBlogPhotoAsync(photoFile, webRootPath, subFolder));
        }

        [Fact]
        public async Task AddBlogPhotoAsync_WithEmptyFile_ThrowsArgumentException()
        {
            // Arrange
            var photoFile = CreateMockFormFile("test.jpg", "");
            photoFile.Setup(_ => _.Length).Returns(0);

            var webRootPath = _tempTestDirectory;
            var subFolder = "blog";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _photoService.AddBlogPhotoAsync(photoFile.Object, webRootPath, subFolder));
        }

        [Fact]
        public async Task AddBlogPhotoAsync_WithInvalidExtension_ThrowsArgumentException()
        {
            // Arrange
            var photoFile = CreateMockFormFile("test.txt", "Hello World");
            var webRootPath = _tempTestDirectory;
            var subFolder = "blog";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _photoService.AddBlogPhotoAsync(photoFile.Object, webRootPath, subFolder));
        }

        [Fact]
        public async Task AddBlogPhotoAsync_WithTooLargeFile_ThrowsArgumentException()
        {
            // Arrange
            var photoFile = CreateMockFormFile("test.jpg", new string('a', 11 * 1024 * 1024)); // 11 MB
            var webRootPath = _tempTestDirectory;
            var subFolder = "blog";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _photoService.AddBlogPhotoAsync(photoFile.Object, webRootPath, subFolder));
        }

        [Fact]
        public async Task AddBlogPhotoAsync_WithNullAltText_UsesFileNameAsAlt()
        {
            // Arrange
            var photoFile = CreateMockFormFile("test-image.jpg", "Hello World");
            var webRootPath = _tempTestDirectory;
            var subFolder = "blog";

            _photoRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Photo>()))
                .ReturnsAsync((Photo p) => p);

            // Act
            var result = await _photoService.AddBlogPhotoAsync(photoFile.Object, webRootPath, subFolder, altText: null);

            // Assert
            Assert.Equal("test-image", result.Alt); // Should use filename without extension
        }

        #endregion

        #region AddMultipleBlogPhotosAsync Tests

        [Fact]
        public async Task AddMultipleBlogPhotosAsync_WithValidFiles_AddsPhotos()
        {
            // Arrange
            var photoFiles = new FormFileCollection
            {
                CreateMockFormFile("test1.jpg", "Hello World 1").Object,
                CreateMockFormFile("test2.jpg", "Hello World 2").Object
            };

            var webRootPath = _tempTestDirectory;
            var subFolder = "blog";
            var postId = 1;
            var commentId = 1;

            _photoRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Photo>()))
                .ReturnsAsync((Photo p) => p);

            // Act
            var result = await _photoService.AddMultipleBlogPhotosAsync(photoFiles, webRootPath, subFolder, postId, commentId);

            // Assert
            Assert.Equal(2, result.Count());
            _photoRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Photo>()), Times.Exactly(2));
        }

        [Fact]
        public async Task AddMultipleBlogPhotosAsync_WithSomeInvalidFiles_AddsOnlyValidOnes()
        {
            // Arrange
            var photoFiles = new FormFileCollection
            {
                CreateMockFormFile("test1.jpg", "Hello World 1").Object,
                CreateMockFormFile("test2.txt", "Hello World 2").Object, // Invalid extension
                CreateMockFormFile("test3.jpg", "Hello World 3").Object
            };

            var webRootPath = _tempTestDirectory;
            var subFolder = "blog";

            _photoRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Photo>()))
                .ReturnsAsync((Photo p) => p);

            // Act
            var result = await _photoService.AddMultipleBlogPhotosAsync(photoFiles, webRootPath, subFolder);

            // Assert
            Assert.Equal(2, result.Count()); // Only 2 valid files
            _photoRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Photo>()), Times.Exactly(2));
        }

        [Fact]
        public async Task AddMultipleBlogPhotosAsync_WithEmptyFiles_AddsNothing()
        {
            // Arrange
            var mockFile1 = CreateMockFormFile("test1.jpg", "content");
            mockFile1.Setup(_ => _.Length).Returns(0);

            var mockFile2 = CreateMockFormFile("test2.jpg", "content");
            mockFile2.Setup(_ => _.Length).Returns(0);

            var photoFiles = new FormFileCollection
            {
                mockFile1.Object,
                mockFile2.Object
            };

            var webRootPath = _tempTestDirectory;
            var subFolder = "blog";

            _photoRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Photo>()))
                .ReturnsAsync((Photo p) => p);

            // Act
            var result = await _photoService.AddMultipleBlogPhotosAsync(photoFiles, webRootPath, subFolder);

            // Assert
            Assert.Empty(result);
            _photoRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Photo>()), Times.Never);
        }

        #endregion

        #region DeletePhotoAsync Tests

        [Fact]
        public async Task DeletePhotoAsync_WithExistingPhoto_DeletesPhotoAndFile()
        {
            // Arrange
            var photoId = 1;
            var webRootPath = _tempTestDirectory;

            // Create test file
            var testFilePath = Path.Combine(webRootPath, "images", "blog", "test.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(testFilePath));
            await File.WriteAllBytesAsync(testFilePath, new byte[100]);

            var photo = new Photo
            {
                Id = photoId,
                FilePath = "/images/blog/test.jpg",
                Name = "test"
            };

            _photoRepositoryMock
                .Setup(x => x.GetByIdAsync(photoId))
                .ReturnsAsync(photo);
            _photoRepositoryMock
                .Setup(x => x.DeleteAsync(photo))
                .Returns(Task.CompletedTask);

            // Act
            await _photoService.DeletePhotoAsync(photoId, webRootPath);

            // Assert
            _photoRepositoryMock.Verify(x => x.DeleteAsync(photo), Times.Once);
            Assert.False(File.Exists(testFilePath)); // File should be deleted
        }

        [Fact]
        public async Task DeletePhotoAsync_WithNonExistingPhoto_DoesNothing()
        {
            // Arrange
            var photoId = 999;
            var webRootPath = _tempTestDirectory;

            _photoRepositoryMock
                .Setup(x => x.GetByIdAsync(photoId))
                .ReturnsAsync((Photo)null);

            // Act
            await _photoService.DeletePhotoAsync(photoId, webRootPath);

            // Assert
            _photoRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Photo>()), Times.Never);
        }

        #endregion

        #region DeletePhotoFileAsync Tests

        [Fact]
        public async Task DeletePhotoFileAsync_WithExistingFile_DeletesFile()
        {
            // Arrange
            var filePath = "/images/blog/test.jpg";
            var webRootPath = _tempTestDirectory;

            // Create test file
            var fullPath = Path.Combine(webRootPath, filePath.TrimStart('/'));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            await File.WriteAllBytesAsync(fullPath, new byte[100]);

            // Act
            await _photoService.DeletePhotoFileAsync(filePath, webRootPath);

            // Assert
            Assert.False(File.Exists(fullPath));
        }

        [Fact]
        public async Task DeletePhotoFileAsync_WithNonExistingFile_CompletesWithoutError()
        {
            // Arrange
            var filePath = "/images/blog/nonexistent.jpg";
            var webRootPath = _tempTestDirectory;

            // Act & Assert - Should not throw
            await _photoService.DeletePhotoFileAsync(filePath, webRootPath);
        }

        #endregion

        #region SavePhotoAsync Tests

        [Fact]
        public async Task SavePhotoAsync_WithValidFile_ReturnsFilePath()
        {
            // Arrange
            var photoFile = CreateMockFormFile("test.jpg", "Hello World from a Fake File");
            var webRootPath = _tempTestDirectory;
            var subFolder = "blog";

            // Act
            var result = await _photoService.SavePhotoAsync(photoFile.Object, webRootPath, subFolder);

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith($"/images/{subFolder}/", result);
            Assert.Contains("test.jpg", result);

            // Verify file was actually created
            var fullPath = Path.Combine(webRootPath, result.TrimStart('/'));
            Assert.True(File.Exists(fullPath));
        }

        [Fact]
        public async Task SavePhotoAsync_WithNullFile_ThrowsArgumentException()
        {
            // Arrange
            IFormFile photoFile = null;
            var webRootPath = _tempTestDirectory;
            var subFolder = "blog";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _photoService.SavePhotoAsync(photoFile, webRootPath, subFolder));
        }

        [Fact]
        public async Task SavePhotoAsync_WithEmptyFile_ThrowsArgumentException()
        {
            // Arrange
            var photoFile = CreateMockFormFile("test.jpg", "");
            photoFile.Setup(_ => _.Length).Returns(0);

            var webRootPath = _tempTestDirectory;
            var subFolder = "blog";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _photoService.SavePhotoAsync(photoFile.Object, webRootPath, subFolder));
        }

        [Fact]
        public async Task SavePhotoAsync_WithInvalidExtension_ThrowsArgumentException()
        {
            // Arrange
            var photoFile = CreateMockFormFile("test.txt", "Hello World");
            var webRootPath = _tempTestDirectory;
            var subFolder = "blog";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _photoService.SavePhotoAsync(photoFile.Object, webRootPath, subFolder));
        }

        [Fact]
        public async Task SavePhotoAsync_WithTooLargeFile_ThrowsArgumentException()
        {
            // Arrange
            var photoFile = CreateMockFormFile("test.jpg", new string('a', 11 * 1024 * 1024)); // 11 MB
            var webRootPath = _tempTestDirectory;
            var subFolder = "blog";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _photoService.SavePhotoAsync(photoFile.Object, webRootPath, subFolder));
        }

        #endregion

        #region Helper Methods

        private Mock<IFormFile> CreateMockFormFile(string fileName, string content)
        {
            var fileMock = new Mock<IFormFile>();
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var ms = new MemoryStream(contentBytes);

            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(ms.Length);
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");

            return fileMock;
        }

        private Mock<IFormFile> CreateMockFormFile(string fileName, byte[] content)
        {
            var fileMock = new Mock<IFormFile>();
            var ms = new MemoryStream(content);

            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(ms.Length);
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");

            return fileMock;
        }

        // Helper class for testing non-seekable streams
        private class NonSeekableStream : MemoryStream
        {
            public NonSeekableStream(byte[] buffer) : base(buffer) { }

            public override bool CanSeek => false;

            public override long Length => throw new NotSupportedException();

            public override long Seek(long offset, SeekOrigin loc)
            {
                throw new NotSupportedException();
            }
        }

        #endregion
    }
}