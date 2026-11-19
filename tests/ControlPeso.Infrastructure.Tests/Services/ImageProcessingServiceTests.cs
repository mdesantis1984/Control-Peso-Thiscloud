using ControlPeso.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SkiaSharp;

namespace ControlPeso.Infrastructure.Tests.Services;

/// <summary>
/// Tests para ImageProcessingService (SkiaSharp)
/// </summary>
public class ImageProcessingServiceTests
{
    private static ImageProcessingService CreateService(Mock<ILogger<ImageProcessingService>>? loggerMock = null)
    {
        loggerMock ??= new Mock<ILogger<ImageProcessingService>>();
        return new ImageProcessingService(loggerMock.Object);
    }

    private static MemoryStream CreateTestImage(
        int width,
        int height,
        SKEncodedImageFormat format = SKEncodedImageFormat.Jpeg)
        => ToStream(FillBitmap(width, height, (_, _) => SKColors.Red), format);

    /// <summary>Builds a bitmap whose pixels come from <paramref name="color"/> (x, y).</summary>
    private static SKBitmap FillBitmap(int width, int height, Func<int, int, SKColor> color)
    {
        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                bitmap.SetPixel(x, y, color(x, y));
            }
        }

        return bitmap;
    }

    private static MemoryStream ToStream(SKBitmap bitmap, SKEncodedImageFormat format, int quality = 95)
    {
        using (bitmap)
        using (var image = SKImage.FromBitmap(bitmap))
        using (var data = image.Encode(format, quality))
        {
            var stream = new MemoryStream();
            data.SaveTo(stream);
            stream.Position = 0;
            return stream;
        }
    }

    /// <summary>
    /// Inserts an EXIF APP1 segment carrying the given orientation right after the JPEG SOI marker.
    /// </summary>
    private static MemoryStream WithExifOrientation(MemoryStream jpeg, byte orientation)
    {
        byte[] app1 =
        [
            0xFF, 0xE1, 0x00, 0x22,
            0x45, 0x78, 0x69, 0x66, 0x00, 0x00,             // "Exif\0\0"
            0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00, // TIFF header, little endian, IFD0 at 8
            0x01, 0x00,                                     // one IFD entry
            0x12, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, orientation, 0x00, 0x00, 0x00, // Orientation (SHORT)
            0x00, 0x00, 0x00, 0x00                          // no next IFD
        ];

        var source = jpeg.ToArray();
        var result = new MemoryStream();
        result.Write(source, 0, 2); // SOI
        result.Write(app1, 0, app1.Length);
        result.Write(source, 2, source.Length - 2);
        result.Position = 0;
        return result;
    }

    private static MemoryStream CreateGif()
        => new(Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"));

    /// <summary>Builds a 2x2, 24-bit BMP.</summary>
    private static MemoryStream CreateBmp()
    {
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write((byte)'B');
            writer.Write((byte)'M');
            writer.Write(70);        // file size: 54 header + 2 rows * 8 bytes
            writer.Write(0);         // reserved
            writer.Write(54);        // pixel data offset
            writer.Write(40);        // DIB header size
            writer.Write(2);         // width
            writer.Write(2);         // height
            writer.Write((short)1);  // planes
            writer.Write((short)24); // bits per pixel
            writer.Write(0);         // compression
            writer.Write(16);        // image size
            writer.Write(2835);
            writer.Write(2835);
            writer.Write(0);
            writer.Write(0);
            for (var row = 0; row < 2; row++)
            {
                writer.Write(new byte[] { 0, 0, 255, 0, 0, 255, 0, 0 }); // 2 red pixels + 2 bytes padding
            }
        }

        stream.Position = 0;
        return stream;
    }

    private static byte[] Noise(int width, int height, SKEncodedImageFormat format)
    {
        var random = new Random(1234);
        using var bitmap = FillBitmap(width, height, (_, _) =>
            new SKColor((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256)));
        return ToStream(bitmap, format, 100).ToArray();
    }

    private static void AssertIsWebP(byte[] data)
    {
        data.Length.Should().BeGreaterThan(12);
        System.Text.Encoding.ASCII.GetString(data, 0, 4).Should().Be("RIFF");
        System.Text.Encoding.ASCII.GetString(data, 8, 4).Should().Be("WEBP");
    }

    private static SKBitmap DecodeOutput(MemoryStream output)
    {
        AssertIsWebP(output.ToArray());
        return SKBitmap.Decode(output.ToArray()) ?? throw new InvalidOperationException("Output is not decodable");
    }

    private static bool IsRed(SKColor c) => c.Red > 170 && c.Green < 90 && c.Blue < 90;
    private static bool IsBlue(SKColor c) => c.Blue > 170 && c.Red < 90 && c.Green < 90;
    private static bool IsGreen(SKColor c) => c.Green > 140 && c.Red < 90 && c.Blue < 90;

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ImageProcessingService(null!));
    }

    #endregion

    #region ProcessAvatarImageAsync Tests

    [Fact]
    public async Task ProcessAvatarImageAsync_WithNullInputStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = CreateService();
        using var outputStream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.ProcessAvatarImageAsync(null!, outputStream));
    }

    [Fact]
    public async Task ProcessAvatarImageAsync_WithNullOutputStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = CreateService();
        using var inputStream = CreateTestImage(512, 512);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.ProcessAvatarImageAsync(inputStream, null!));
    }

    [Fact]
    public async Task ProcessAvatarImageAsync_WithInvalidTargetSize_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var service = CreateService();
        using var inputStream = CreateTestImage(512, 512);
        using var outputStream = new MemoryStream();

        // Act & Assert - targetSize too small (< 64)
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.ProcessAvatarImageAsync(inputStream, outputStream, targetSize: 63));

        inputStream.Position = 0;

        // Act & Assert - targetSize too large (> 2048)
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.ProcessAvatarImageAsync(inputStream, outputStream, targetSize: 2049));
    }

    [Fact]
    public async Task ProcessAvatarImageAsync_WithInvalidQuality_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var service = CreateService();
        using var inputStream = CreateTestImage(512, 512);
        using var outputStream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.ProcessAvatarImageAsync(inputStream, outputStream, quality: 0)); // Too low

        inputStream.Position = 0;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.ProcessAvatarImageAsync(inputStream, outputStream, quality: 101)); // Too high
    }

    [Fact]
    public async Task ProcessAvatarImageAsync_WithValidImage_ShouldProcessSuccessfully()
    {
        // Arrange
        var service = CreateService();
        using var inputStream = CreateTestImage(1024, 1024);
        using var outputStream = new MemoryStream();

        // Act
        await service.ProcessAvatarImageAsync(inputStream, outputStream, targetSize: 512, quality: 85);

        // Assert
        outputStream.Length.Should().BeGreaterThan(0);
        outputStream.Position = 0;

        // Verify output is a valid WebP image
        using var processedImage = DecodeOutput(outputStream);
        processedImage.Width.Should().Be(512);
        processedImage.Height.Should().Be(512);
    }

    [Fact]
    public async Task ProcessAvatarImageAsync_WithNonSquareImage_ShouldCropToSquare()
    {
        // Arrange
        var service = CreateService();
        using var inputStream = CreateTestImage(1920, 1080); // 16:9 aspect ratio
        using var outputStream = new MemoryStream();

        // Act
        await service.ProcessAvatarImageAsync(inputStream, outputStream, targetSize: 512);

        // Assert
        using var processedImage = DecodeOutput(outputStream);
        processedImage.Width.Should().Be(512);
        processedImage.Height.Should().Be(512);
    }

    [Fact]
    public async Task ProcessAvatarImageAsync_WithTooSmallImage_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        using var inputStream = CreateTestImage(20, 20); // Below minimum 32x32
        using var outputStream = new MemoryStream();

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ProcessAvatarImageAsync(inputStream, outputStream));

        // Assert
        ex.Message.Should().Be("Failed to process avatar image");
        ex.InnerException.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Be("Image too small (min 32x32px). Actual: 20x20");
        outputStream.Length.Should().Be(0);
    }

    [Fact]
    public async Task ProcessAvatarImageAsync_WithMinimumSizeImage_ShouldProcessSuccessfully()
    {
        // Arrange
        var service = CreateService();
        using var inputStream = CreateTestImage(32, 32);
        using var outputStream = new MemoryStream();

        // Act
        await service.ProcessAvatarImageAsync(inputStream, outputStream, targetSize: 64);

        // Assert (small images are scaled up to the target size)
        using var processedImage = DecodeOutput(outputStream);
        processedImage.Width.Should().Be(64);
        processedImage.Height.Should().Be(64);
    }

    [Fact]
    public async Task ProcessAvatarImageAsync_WithInvalidImageData_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        using var inputStream = new MemoryStream([1, 2, 3, 4, 5]); // Invalid image data
        using var outputStream = new MemoryStream();

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ProcessAvatarImageAsync(inputStream, outputStream));

        // Assert
        ex.Message.Should().StartWith("Invalid image format. Supported:");
        outputStream.Length.Should().Be(0);
    }

    [Fact]
    public async Task ProcessAvatarImageAsync_WithTruncatedImage_ShouldThrowCorruptedImageError()
    {
        // Arrange: valid PNG header, but the pixel data is cut off
        var service = CreateService();
        var png = CreateTestImage(256, 256, SKEncodedImageFormat.Png).ToArray();
        using var inputStream = new MemoryStream(png, 0, 60);
        using var outputStream = new MemoryStream();

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ProcessAvatarImageAsync(inputStream, outputStream));

        // Assert
        ex.Message.Should().Be("Image file is corrupted or invalid");
        outputStream.Length.Should().Be(0);
    }

    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public async Task ProcessAvatarImageAsync_WithSupportedInputFormat_ShouldProduceWebP(SKEncodedImageFormat format)
    {
        // Arrange
        var service = CreateService();
        using var inputStream = CreateTestImage(300, 200, format);
        using var outputStream = new MemoryStream();

        // Act
        await service.ProcessAvatarImageAsync(inputStream, outputStream, targetSize: 128);

        // Assert
        using var processedImage = DecodeOutput(outputStream);
        processedImage.Width.Should().Be(128);
        processedImage.Height.Should().Be(128);
    }

    [Fact]
    public async Task ProcessAvatarImageAsync_WithGifAndBmpInput_ShouldProduceWebP()
    {
        // Arrange
        var service = CreateService();
        using var gif = CreateGif();
        using var bmp = CreateBmp();
        using var gifOutput = new MemoryStream();
        using var bmpOutput = new MemoryStream();

        // Act & Assert: 1x1 / 2x2 images are below the 32x32 minimum, so they are rejected
        // only for their size, which proves they were decoded as valid images.
        var gifEx = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ProcessAvatarImageAsync(gif, gifOutput));
        gifEx.InnerException!.Message.Should().Be("Image too small (min 32x32px). Actual: 1x1");

        var bmpEx = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ProcessAvatarImageAsync(bmp, bmpOutput));
        bmpEx.InnerException!.Message.Should().Be("Image too small (min 32x32px). Actual: 2x2");
    }

    [Fact]
    public async Task ProcessAvatarImageAsync_WithWideImage_ShouldKeepCenterSquare()
    {
        // Arrange: 200x100, green bands on the sides (50px each) and red in the center 100x100
        var service = CreateService();
        using var inputStream = ToStream(
            FillBitmap(200, 100, (x, _) => x < 50 || x >= 150 ? SKColors.Lime : SKColors.Red),
            SKEncodedImageFormat.Png);
        using var outputStream = new MemoryStream();

        // Act
        await service.ProcessAvatarImageAsync(inputStream, outputStream, targetSize: 64);

        // Assert: the whole output is the red center, none of the green sides
        using var processedImage = DecodeOutput(outputStream);
        processedImage.Width.Should().Be(64);
        processedImage.Height.Should().Be(64);
        IsRed(processedImage.GetPixel(2, 32)).Should().BeTrue();
        IsRed(processedImage.GetPixel(32, 32)).Should().BeTrue();
        IsRed(processedImage.GetPixel(61, 32)).Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAvatarImageAsync_WithTallImage_ShouldKeepCenterSquare()
    {
        // Arrange: 100x200, green bands top and bottom (50px each), red in the middle
        var service = CreateService();
        using var inputStream = ToStream(
            FillBitmap(100, 200, (_, y) => y < 50 || y >= 150 ? SKColors.Lime : SKColors.Red),
            SKEncodedImageFormat.Png);
        using var outputStream = new MemoryStream();

        // Act
        await service.ProcessAvatarImageAsync(inputStream, outputStream, targetSize: 64);

        // Assert
        using var processedImage = DecodeOutput(outputStream);
        IsRed(processedImage.GetPixel(32, 2)).Should().BeTrue();
        IsRed(processedImage.GetPixel(32, 61)).Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAvatarImageAsync_WithExifOrientation_ShouldRotateBeforeCropping()
    {
        // Arrange: raw 100x200 JPEG, top half red / bottom half blue, EXIF orientation 6 (rotate 90° CW).
        // Displayed: 200x100, left half blue / right half red.
        var service = CreateService();
        using var raw = ToStream(
            FillBitmap(100, 200, (_, y) => y < 100 ? SKColors.Red : SKColors.Blue),
            SKEncodedImageFormat.Jpeg);
        using var inputStream = WithExifOrientation(raw, orientation: 6);
        using var outputStream = new MemoryStream();

        // Act
        await service.ProcessAvatarImageAsync(inputStream, outputStream, targetSize: 64);

        // Assert
        using var processedImage = DecodeOutput(outputStream);
        processedImage.Width.Should().Be(64);
        processedImage.Height.Should().Be(64);
        IsBlue(processedImage.GetPixel(4, 32)).Should().BeTrue();
        IsRed(processedImage.GetPixel(60, 32)).Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAvatarImageAsync_WithTransparentPng_ShouldKeepTransparency()
    {
        // Arrange: left half fully transparent, right half opaque red
        var service = CreateService();
        var bitmap = new SKBitmap(100, 100, SKColorType.Rgba8888, SKAlphaType.Premul);
        for (var y = 0; y < 100; y++)
        {
            for (var x = 0; x < 100; x++)
            {
                bitmap.SetPixel(x, y, x < 50 ? SKColors.Transparent : SKColors.Red);
            }
        }

        using var inputStream = ToStream(bitmap, SKEncodedImageFormat.Png);
        using var outputStream = new MemoryStream();

        // Act
        await service.ProcessAvatarImageAsync(inputStream, outputStream, targetSize: 64);

        // Assert
        using var processedImage = DecodeOutput(outputStream);
        processedImage.GetPixel(4, 32).Alpha.Should().BeLessThan(30);
        processedImage.GetPixel(60, 32).Alpha.Should().BeGreaterThan(225);
    }

    [Fact]
    public async Task ProcessAvatarImageAsync_WithLowerQuality_ShouldProduceSmallerOutput()
    {
        // Arrange
        var service = CreateService();
        var noise = Noise(256, 256, SKEncodedImageFormat.Png);
        using var lowInput = new MemoryStream(noise);
        using var highInput = new MemoryStream(noise);
        using var lowOutput = new MemoryStream();
        using var highOutput = new MemoryStream();

        // Act
        await service.ProcessAvatarImageAsync(lowInput, lowOutput, targetSize: 256, quality: 10);
        await service.ProcessAvatarImageAsync(highInput, highOutput, targetSize: 256, quality: 95);

        // Assert
        AssertIsWebP(lowOutput.ToArray());
        AssertIsWebP(highOutput.ToArray());
        lowOutput.Length.Should().BeLessThan(highOutput.Length);
    }

    [Fact]
    public async Task ProcessAvatarImageAsync_WithQuality100_ShouldStillProduceLossyWebP()
    {
        // Arrange: noise at quality 100 would be several times larger if it were encoded lossless
        var service = CreateService();
        var noise = Noise(256, 256, SKEncodedImageFormat.Png);
        using var input100 = new MemoryStream(noise);
        using var input90 = new MemoryStream(noise);
        using var output100 = new MemoryStream();
        using var output90 = new MemoryStream();

        // Act
        await service.ProcessAvatarImageAsync(input100, output100, targetSize: 256, quality: 100);
        await service.ProcessAvatarImageAsync(input90, output90, targetSize: 256, quality: 90);

        // Assert (lossless noise would be ~196 KB, lossy stays far below)
        output100.Length.Should().BeLessThan(150_000);
        output100.Length.Should().BeGreaterThan(output90.Length);
    }

    [Fact]
    public async Task ProcessAvatarImageAsync_WithValidImage_ShouldNotCloseStreams()
    {
        // Arrange
        var service = CreateService();
        using var inputStream = CreateTestImage(128, 128);
        using var outputStream = new MemoryStream();

        // Act
        await service.ProcessAvatarImageAsync(inputStream, outputStream, targetSize: 64);

        // Assert
        inputStream.CanRead.Should().BeTrue();
        outputStream.CanWrite.Should().BeTrue();
    }

    #endregion

    #region IsValidImageAsync Tests

    [Fact]
    public async Task IsValidImageAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.IsValidImageAsync(null!));
    }

    [Fact]
    public async Task IsValidImageAsync_WithNonSeekableStream_ShouldThrowArgumentException()
    {
        // Arrange
        var service = CreateService();
        var nonSeekableStream = new Mock<Stream>();
        nonSeekableStream.Setup(s => s.CanSeek).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.IsValidImageAsync(nonSeekableStream.Object));
    }

    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public async Task IsValidImageAsync_WithSupportedImage_ShouldReturnTrue(SKEncodedImageFormat format)
    {
        // Arrange
        var service = CreateService();
        using var stream = CreateTestImage(64, 64, format);

        // Act
        var result = await service.IsValidImageAsync(stream);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsValidImageAsync_WithGifAndBmp_ShouldReturnTrue()
    {
        // Arrange
        var service = CreateService();
        using var gif = CreateGif();
        using var bmp = CreateBmp();

        // Act & Assert
        (await service.IsValidImageAsync(gif)).Should().BeTrue();
        (await service.IsValidImageAsync(bmp)).Should().BeTrue();
    }

    [Fact]
    public async Task IsValidImageAsync_WithEmptyStream_ShouldReturnFalse()
    {
        // Arrange
        var service = CreateService();
        using var stream = new MemoryStream();

        // Act & Assert
        (await service.IsValidImageAsync(stream)).Should().BeFalse();
    }

    [Fact]
    public async Task IsValidImageAsync_WithImageFormatOutsideSupportedList_ShouldReturnFalse()
    {
        // Arrange: a minimal ICO file is a valid image for Skia but not a supported avatar format
        var service = CreateService();
        byte[] ico =
        [
            0x00, 0x00, 0x01, 0x00, 0x01, 0x00,
            0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x20, 0x00, 0x30, 0x00, 0x00, 0x00, 0x16, 0x00, 0x00, 0x00,
            0x28, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01, 0x00, 0x20, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00
        ];
        using var stream = new MemoryStream(ico);

        // Act & Assert
        (await service.IsValidImageAsync(stream)).Should().BeFalse();
    }

    [Fact]
    public async Task IsValidImageAsync_WithInvalidData_ShouldReturnFalse()
    {
        // Arrange
        var service = CreateService();
        using var stream = new MemoryStream([1, 2, 3, 4, 5]);

        // Act
        var result = await service.IsValidImageAsync(stream);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidImageAsync_ShouldResetStreamPosition()
    {
        // Arrange
        var service = CreateService();
        using var stream = CreateTestImage(64, 64);
        stream.Position = 0;

        // Act
        await service.IsValidImageAsync(stream);

        // Assert
        stream.Position.Should().Be(0);
    }

    [Fact]
    public async Task IsValidImageAsync_WithInvalidData_ShouldResetStreamPosition()
    {
        // Arrange
        var service = CreateService();
        using var stream = new MemoryStream([1, 2, 3, 4, 5]);
        stream.Position = 2;

        // Act
        var result = await service.IsValidImageAsync(stream);

        // Assert
        result.Should().BeFalse();
        stream.Position.Should().Be(2);
    }

    #endregion

    #region GetImageFormatAsync Tests

    [Fact]
    public async Task GetImageFormatAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.GetImageFormatAsync(null!));
    }

    [Fact]
    public async Task GetImageFormatAsync_WithNonSeekableStream_ShouldThrowArgumentException()
    {
        // Arrange
        var service = CreateService();
        var nonSeekableStream = new Mock<Stream>();
        nonSeekableStream.Setup(s => s.CanSeek).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetImageFormatAsync(nonSeekableStream.Object));
    }

    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg, "Jpeg")]
    [InlineData(SKEncodedImageFormat.Png, "Png")]
    [InlineData(SKEncodedImageFormat.Webp, "WebP")]
    public async Task GetImageFormatAsync_WithSupportedImage_ShouldReturnFormatName(
        SKEncodedImageFormat format, string expected)
    {
        // Arrange
        var service = CreateService();
        using var stream = CreateTestImage(64, 64, format);

        // Act
        var result = await service.GetImageFormatAsync(stream);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetImageFormatAsync_WithGifAndBmp_ShouldReturnFormatName()
    {
        // Arrange
        var service = CreateService();
        using var gif = CreateGif();
        using var bmp = CreateBmp();

        // Act & Assert
        (await service.GetImageFormatAsync(gif)).Should().Be("Gif");
        (await service.GetImageFormatAsync(bmp)).Should().Be("Bmp");
    }

    [Fact]
    public async Task GetImageFormatAsync_WithEmptyStream_ShouldReturnNull()
    {
        // Arrange
        var service = CreateService();
        using var stream = new MemoryStream();

        // Act & Assert
        (await service.GetImageFormatAsync(stream)).Should().BeNull();
    }

    [Fact]
    public async Task GetImageFormatAsync_WithInvalidData_ShouldReturnNull()
    {
        // Arrange
        var service = CreateService();
        using var stream = new MemoryStream([1, 2, 3, 4, 5]);

        // Act
        var result = await service.GetImageFormatAsync(stream);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetImageFormatAsync_ShouldResetStreamPosition()
    {
        // Arrange
        var service = CreateService();
        using var stream = CreateTestImage(64, 64);

        // Act
        await service.GetImageFormatAsync(stream);

        // Assert
        stream.Position.Should().Be(0);
    }

    #endregion

    [Fact]
    public async Task ProcessAvatarImageAsync_WithTooManyPixels_ShouldRejectBeforeDecoding()
    {
        // Arrange: 6000x6000 = 36 MP, a small PNG that would decode into a ~144 MB bitmap
        var service = CreateService();
        using var bitmap = new SKBitmap(6000, 6000, SKColorType.Rgba8888, SKAlphaType.Opaque);
        bitmap.Erase(SKColors.Red);
        using var inputStream = ToStream(bitmap, SKEncodedImageFormat.Png);
        using var outputStream = new MemoryStream();

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ProcessAvatarImageAsync(inputStream, outputStream));

        // Assert
        ex.Message.Should().Be("Failed to process avatar image");
        ex.InnerException.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Be("Image too large (max 25000000 pixels). Actual: 6000x6000");
        outputStream.Length.Should().Be(0);
    }
}
