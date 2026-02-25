using ControlPeso.Application.Interfaces;
using ControlPeso.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlPeso.Infrastructure.Tests.Services;

/// <summary>
/// Tests for LocalPhotoStorageService
/// Tests file system operations with temporary directories
/// </summary>
public sealed class LocalPhotoStorageServiceTests : IDisposable
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IImageProcessingService> _mockImageProcessingService;
    private readonly Mock<ILogger<LocalPhotoStorageService>> _mockLogger;
    private readonly string _tempDirectory;
    private readonly LocalPhotoStorageService _sut;

    public LocalPhotoStorageServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockImageProcessingService = new Mock<IImageProcessingService>();
        _mockLogger = new Mock<ILogger<LocalPhotoStorageService>>();

        // Create temporary test directory
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"ControlPesoTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);

        var webRootPath = _tempDirectory;
        var uploadsFolder = "uploads/avatars";

        _mockConfiguration.Setup(x => x["PhotoStorage:WebRootPath"]).Returns(webRootPath);
        _mockConfiguration.Setup(x => x["PhotoStorage:AvatarsFolder"]).Returns(uploadsFolder);

        _sut = new LocalPhotoStorageService(
            _mockConfiguration.Object,
            _mockImageProcessingService.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        // Clean up temporary directory
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConfigurationIsNull()
    {
        // Act
        var act = () => new LocalPhotoStorageService(
            null!,
            _mockImageProcessingService.Object,
            _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenImageProcessingServiceIsNull()
    {
        // Act
        var act = () => new LocalPhotoStorageService(
            _mockConfiguration.Object,
            null!,
            _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("imageProcessingService");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new LocalPhotoStorageService(
            _mockConfiguration.Object,
            _mockImageProcessingService.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldCreateUploadsDirectory_WhenItDoesNotExist()
    {
        // Assert
        var uploadsPath = Path.Combine(_tempDirectory, "uploads/avatars");
        Directory.Exists(uploadsPath).Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldUseDefaultPaths_WhenConfigurationIsEmpty()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(x => x["PhotoStorage:WebRootPath"]).Returns((string)null!);
        mockConfig.Setup(x => x["PhotoStorage:AvatarsFolder"]).Returns((string)null!);

        var tempDir = Path.Combine(Path.GetTempPath(), $"ControlPesoTests_Default_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var service = new LocalPhotoStorageService(
                mockConfig.Object,
                _mockImageProcessingService.Object,
                _mockLogger.Object);

            // Assert
            service.Should().NotBeNull();

            // Clean up
            Directory.Delete(tempDir, recursive: true);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    #endregion

    #region SaveUserPhotoAsync Tests

    [Fact]
    public async Task SaveUserPhotoAsync_ShouldThrowArgumentNullException_WhenStreamIsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fileName = "test.jpg";

        // Act
        var act = async () => await _sut.SaveUserPhotoAsync(userId, fileName, null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("stream");
    }

    [Fact]
    public async Task SaveUserPhotoAsync_ShouldThrowArgumentException_WhenUserIdIsEmpty()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3]);
        var fileName = "test.jpg";

        // Act
        var act = async () => await _sut.SaveUserPhotoAsync(Guid.Empty, fileName, stream, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("userId");
    }

    [Fact]
    public async Task SaveUserPhotoAsync_ShouldThrowArgumentException_WhenFileNameIsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        using var stream = new MemoryStream([1, 2, 3]);

        // Act
        var act = async () => await _sut.SaveUserPhotoAsync(userId, null!, stream, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("fileName");
    }

    [Fact]
    public async Task SaveUserPhotoAsync_ShouldThrowArgumentException_WhenFileNameIsEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        using var stream = new MemoryStream([1, 2, 3]);

        // Act
        var act = async () => await _sut.SaveUserPhotoAsync(userId, string.Empty, stream, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("fileName");
    }

    [Fact]
    public async Task SaveUserPhotoAsync_ShouldThrowArgumentException_WhenFileNameIsWhiteSpace()
    {
        // Arrange
        var userId = Guid.NewGuid();
        using var stream = new MemoryStream([1, 2, 3]);

        // Act
        var act = async () => await _sut.SaveUserPhotoAsync(userId, "   ", stream, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("fileName");
    }

    [Fact]
    public async Task SaveUserPhotoAsync_ShouldSavePhoto_WhenValidInputs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fileName = "test-photo.jpg";
        using var inputStream = new MemoryStream([1, 2, 3, 4, 5]);

        // Mock ProcessAvatarImageAsync to copy input to output
        _mockImageProcessingService
            .Setup(x => x.ProcessAvatarImageAsync(
                It.IsAny<Stream>(),
                It.IsAny<Stream>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Callback<Stream, Stream, int, int, CancellationToken>((input, output, size, quality, ct) =>
            {
                // Write some processed data to output stream
                var processedData = new byte[] { 10, 20, 30, 40, 50 };
                output.Write(processedData);
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SaveUserPhotoAsync(userId, fileName, inputStream, CancellationToken.None);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().StartWith("/uploads/avatars/");
        result.Should().Contain(userId.ToString());
        result.Should().EndWith(".webp");

        // Verify file was created
        var uploadsPath = Path.Combine(_tempDirectory, "uploads/avatars");
        var files = Directory.GetFiles(uploadsPath, $"{userId}_*");
        files.Should().HaveCount(1);
    }

    [Fact]
    public async Task SaveUserPhotoAsync_ShouldDeleteExistingPhotos_WhenUserAlreadyHasPhotos()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fileName = "test-photo.jpg";
        var uploadsPath = Path.Combine(_tempDirectory, "uploads/avatars");

        // Create existing photo files for the user
        var existingFile1 = Path.Combine(uploadsPath, $"{userId}_1234567890_old.webp");
        var existingFile2 = Path.Combine(uploadsPath, $"{userId}_9876543210_another.webp");
        await File.WriteAllBytesAsync(existingFile1, [1, 2, 3]);
        await File.WriteAllBytesAsync(existingFile2, [4, 5, 6]);

        using var inputStream = new MemoryStream([10, 20, 30]);

        // Mock ProcessAvatarImageAsync
        _mockImageProcessingService
            .Setup(x => x.ProcessAvatarImageAsync(
                It.IsAny<Stream>(),
                It.IsAny<Stream>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Callback<Stream, Stream, int, int, CancellationToken>((input, output, size, quality, ct) =>
            {
                var processedData = new byte[] { 100, 101, 102 };
                output.Write(processedData);
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SaveUserPhotoAsync(userId, fileName, inputStream, CancellationToken.None);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();

        // Only the new file should exist (old ones deleted)
        var files = Directory.GetFiles(uploadsPath, $"{userId}_*");
        files.Should().HaveCount(1);
        File.Exists(existingFile1).Should().BeFalse();
        File.Exists(existingFile2).Should().BeFalse();
    }

    [Fact]
    public async Task SaveUserPhotoAsync_ShouldSanitizeFileName_WhenFileNameHasInvalidCharacters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fileName = "Tést Phöto!@#$%^&*().jpg"; // Special chars and accents
        using var inputStream = new MemoryStream([1, 2, 3]);

        // Mock ProcessAvatarImageAsync
        _mockImageProcessingService
            .Setup(x => x.ProcessAvatarImageAsync(
                It.IsAny<Stream>(),
                It.IsAny<Stream>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Callback<Stream, Stream, int, int, CancellationToken>((input, output, size, quality, ct) =>
            {
                var processedData = new byte[] { 10, 20, 30 };
                output.Write(processedData);
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SaveUserPhotoAsync(userId, fileName, inputStream, CancellationToken.None);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("test_photo"); // Sanitized (lowercase, no accents, no special chars)
        result.Should().NotContain("Tést");
        result.Should().NotContain("Phöto");
    }

    [Fact]
    public async Task SaveUserPhotoAsync_ShouldCallImageProcessingService_WithCorrectParameters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fileName = "test.jpg";
        using var inputStream = new MemoryStream([1, 2, 3]);

        _mockImageProcessingService
            .Setup(x => x.ProcessAvatarImageAsync(
                It.IsAny<Stream>(),
                It.IsAny<Stream>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Callback<Stream, Stream, int, int, CancellationToken>((input, output, size, quality, ct) =>
            {
                output.Write(new byte[] { 10, 20, 30 });
            })
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SaveUserPhotoAsync(userId, fileName, inputStream, CancellationToken.None);

        // Assert
        _mockImageProcessingService.Verify(
            x => x.ProcessAvatarImageAsync(
                It.IsAny<Stream>(),
                It.IsAny<Stream>(),
                512,  // targetSize
                85,   // quality
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region DeleteUserPhotosAsync Tests

    [Fact]
    public async Task DeleteUserPhotosAsync_ShouldThrowArgumentException_WhenUserIdIsEmpty()
    {
        // Act
        var act = async () => await _sut.DeleteUserPhotosAsync(Guid.Empty, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("userId");
    }

    [Fact]
    public async Task DeleteUserPhotosAsync_ShouldReturnZero_WhenNoPhotosExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _sut.DeleteUserPhotosAsync(userId, CancellationToken.None);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task DeleteUserPhotosAsync_ShouldDeleteAllUserPhotos_WhenPhotosExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var uploadsPath = Path.Combine(_tempDirectory, "uploads/avatars");

        // Create test photo files
        var file1 = Path.Combine(uploadsPath, $"{userId}_1234567890_photo1.webp");
        var file2 = Path.Combine(uploadsPath, $"{userId}_9876543210_photo2.webp");
        var file3 = Path.Combine(uploadsPath, $"{userId}_5555555555_photo3.webp");
        await File.WriteAllBytesAsync(file1, [1, 2, 3]);
        await File.WriteAllBytesAsync(file2, [4, 5, 6]);
        await File.WriteAllBytesAsync(file3, [7, 8, 9]);

        // Act
        var result = await _sut.DeleteUserPhotosAsync(userId, CancellationToken.None);

        // Assert
        result.Should().Be(3);
        File.Exists(file1).Should().BeFalse();
        File.Exists(file2).Should().BeFalse();
        File.Exists(file3).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUserPhotosAsync_ShouldOnlyDeleteUserPhotos_WhenOtherUsersHavePhotos()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var uploadsPath = Path.Combine(_tempDirectory, "uploads/avatars");

        // Create photos for both users
        var user1File = Path.Combine(uploadsPath, $"{userId1}_1234567890_photo.webp");
        var user2File = Path.Combine(uploadsPath, $"{userId2}_9876543210_photo.webp");
        await File.WriteAllBytesAsync(user1File, [1, 2, 3]);
        await File.WriteAllBytesAsync(user2File, [4, 5, 6]);

        // Act
        var result = await _sut.DeleteUserPhotosAsync(userId1, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        File.Exists(user1File).Should().BeFalse();
        File.Exists(user2File).Should().BeTrue(); // Other user's photo should remain
    }

    #endregion

    #region PhotoExists Tests

    [Fact]
    public void PhotoExists_ShouldReturnFalse_WhenPhotoUrlIsNull()
    {
        // Act
        var result = _sut.PhotoExists(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void PhotoExists_ShouldReturnFalse_WhenPhotoUrlIsEmpty()
    {
        // Act
        var result = _sut.PhotoExists(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void PhotoExists_ShouldReturnFalse_WhenPhotoUrlIsWhiteSpace()
    {
        // Act
        var result = _sut.PhotoExists("   ");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void PhotoExists_ShouldReturnFalse_WhenPhotoDoesNotExist()
    {
        // Arrange
        var photoUrl = "/uploads/avatars/nonexistent.webp";

        // Act
        var result = _sut.PhotoExists(photoUrl);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task PhotoExists_ShouldReturnTrue_WhenPhotoExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fileName = "test_1234567890_photo.webp";
        var uploadsPath = Path.Combine(_tempDirectory, "uploads/avatars");
        var filePath = Path.Combine(uploadsPath, fileName);
        await File.WriteAllBytesAsync(filePath, [1, 2, 3]);

        var photoUrl = $"/uploads/avatars/{fileName}";

        // Act
        var result = _sut.PhotoExists(photoUrl);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PhotoExists_ShouldHandleQueryString_WhenPhotoUrlHasCacheBuster()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fileName = "test_1234567890_photo.webp";
        var uploadsPath = Path.Combine(_tempDirectory, "uploads/avatars");
        var filePath = Path.Combine(uploadsPath, fileName);
        await File.WriteAllBytesAsync(filePath, [1, 2, 3]);

        var photoUrl = $"/uploads/avatars/{fileName}?v=12345";

        // Act
        var result = _sut.PhotoExists(photoUrl);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PhotoExists_ShouldReturnFalse_WhenPathIsInvalid()
    {
        // Arrange
        var photoUrl = "/uploads/avatars/<invalid>?file.webp"; // Invalid path characters

        // Act
        var result = _sut.PhotoExists(photoUrl);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
