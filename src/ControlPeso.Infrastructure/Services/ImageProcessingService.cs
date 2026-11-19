using ControlPeso.Application.Interfaces;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace ControlPeso.Infrastructure.Services;

/// <summary>
/// Image processing service using SkiaSharp (MIT) for avatar optimization.
/// Handles format detection, EXIF orientation, square crop, resize and WebP compression.
/// </summary>
public sealed class ImageProcessingService : IImageProcessingService
{
    private const int MinSourceDimension = 32;

    private readonly ILogger<ImageProcessingService> _logger;

    private static readonly HashSet<SKEncodedImageFormat> SupportedFormats =
    [
        SKEncodedImageFormat.Jpeg,
        SKEncodedImageFormat.Png,
        SKEncodedImageFormat.Gif,
        SKEncodedImageFormat.Bmp,
        SKEncodedImageFormat.Webp
    ];

    public ImageProcessingService(ILogger<ImageProcessingService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public async Task ProcessAvatarImageAsync(
        Stream inputStream,
        Stream outputStream,
        int targetSize = 512,
        int quality = 85,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(inputStream);
        ArgumentNullException.ThrowIfNull(outputStream);

        if (targetSize < 64 || targetSize > 2048)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetSize),
                targetSize,
                "Target size must be between 64 and 2048 pixels");
        }

        if (quality < 1 || quality > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quality),
                quality,
                "Quality must be between 1 and 100");
        }

        _logger.LogInformation(
            "Processing avatar image - TargetSize: {TargetSize}px, Quality: {Quality}",
            targetSize, quality);

        try
        {
            var source = await ReadAllBytesAsync(inputStream, ct);

            byte[] webp;
            using (var data = SKData.CreateCopy(source))
            using (var codec = SKCodec.Create(data))
            {
                // Detect the format from the file content (magic bytes), never from a file name
                if (codec is null || !SupportedFormats.Contains(codec.EncodedFormat))
                {
                    throw new UnknownImageFormatException();
                }

                var width = codec.Info.Width;
                var height = codec.Info.Height;

                _logger.LogDebug(
                    "Image loaded successfully - OriginalSize: {Width}x{Height}, Format: {Format}",
                    width, height, FormatName(codec.EncodedFormat));

                // Validate minimum dimensions
                if (width < MinSourceDimension || height < MinSourceDimension)
                {
                    throw new InvalidOperationException(
                        $"Image too small (min 32x32px). Actual: {width}x{height}");
                }

                ct.ThrowIfCancellationRequested();

                using var decoded = Decode(codec);
                using var oriented = ApplyOrigin(decoded, codec.EncodedOrigin);
                using var avatar = CropAndResize(oriented, targetSize);

                _logger.LogDebug(
                    "Image processed successfully - FinalSize: {Width}x{Height}",
                    avatar.Width, avatar.Height);

                // Save as WebP (lossy) with the specified quality
                using var pixmap = avatar.PeekPixels();
                using var encoded = pixmap.Encode(new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossy, quality));
                if (encoded is null)
                {
                    throw new InvalidImageContentException();
                }

                webp = encoded.ToArray();
            }

            await outputStream.WriteAsync(webp, ct);

            _logger.LogInformation(
                "Avatar image processed successfully - Size: {Size}px, Quality: {Quality}, OutputFormat: WebP",
                targetSize, quality);
        }
        catch (UnknownImageFormatException ex)
        {
            _logger.LogError(ex, "Invalid image format - unable to decode");
            throw new InvalidOperationException("Invalid image format. Supported: JPEG, PNG, GIF, BMP, WebP", ex);
        }
        catch (InvalidImageContentException ex)
        {
            _logger.LogError(ex, "Corrupted or invalid image content");
            throw new InvalidOperationException("Image file is corrupted or invalid", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing avatar image");
            throw new InvalidOperationException("Failed to process avatar image", ex);
        }
    }

    public async Task<bool> IsValidImageAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanSeek)
        {
            throw new ArgumentException("Stream must be seekable for validation", nameof(stream));
        }

        var originalPosition = stream.Position;

        try
        {
            _logger.LogDebug("Validating image stream");

            var format = await DetectFormatAsync(stream, ct);

            if (format is null)
            {
                _logger.LogWarning("Unable to detect image format - invalid image");
                return false;
            }

            var isSupported = SupportedFormats.Contains(format.Value);

            _logger.LogDebug(
                "Image format detected: {Format}, Supported: {IsSupported}",
                FormatName(format.Value), isSupported);

            return isSupported;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating image stream");
            return false;
        }
        finally
        {
            // Reset stream position for subsequent reads
            stream.Position = originalPosition;
        }
    }

    public async Task<string?> GetImageFormatAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanSeek)
        {
            throw new ArgumentException("Stream must be seekable", nameof(stream));
        }

        var originalPosition = stream.Position;

        try
        {
            var format = await DetectFormatAsync(stream, ct);

            return format is null ? null : FormatName(format.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting image format");
            return null;
        }
        finally
        {
            // Reset stream position
            stream.Position = originalPosition;
        }
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }

    /// <summary>Detects the encoded format from the stream content, or null when it is not a decodable image.</summary>
    private static async Task<SKEncodedImageFormat?> DetectFormatAsync(Stream stream, CancellationToken ct)
    {
        var bytes = await ReadAllBytesAsync(stream, ct);
        if (bytes.Length == 0)
        {
            return null;
        }

        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);
        return codec?.EncodedFormat;
    }

    /// <summary>ImageSharp-compatible format names, kept because callers and tests rely on them.</summary>
    private static string FormatName(SKEncodedImageFormat format) => format switch
    {
        SKEncodedImageFormat.Jpeg => "Jpeg",
        SKEncodedImageFormat.Png => "Png",
        SKEncodedImageFormat.Gif => "Gif",
        SKEncodedImageFormat.Bmp => "Bmp",
        SKEncodedImageFormat.Webp => "WebP",
        _ => format.ToString()
    };

    private static SKBitmap Decode(SKCodec codec)
    {
        var info = codec.Info;
        var alpha = info.AlphaType == SKAlphaType.Opaque ? SKAlphaType.Opaque : SKAlphaType.Premul;
        var target = new SKImageInfo(info.Width, info.Height, SKColorType.Rgba8888, alpha);

        var bitmap = new SKBitmap(target);
        try
        {
            var result = codec.GetPixels(target, bitmap.GetPixels());
            if (result != SKCodecResult.Success)
            {
                throw new InvalidImageContentException();
            }

            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    /// <summary>Bakes the EXIF orientation into the pixels (the WebP output carries no EXIF).</summary>
    private static SKBitmap ApplyOrigin(SKBitmap source, SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft)
        {
            return source.Copy();
        }

        float w = source.Width;
        float h = source.Height;
        var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

        var matrix = origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1),
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),
            _ => SKMatrix.Identity
        };

        var info = new SKImageInfo(
            swap ? source.Height : source.Width,
            swap ? source.Width : source.Height,
            SKColorType.Rgba8888,
            source.AlphaType);

        var result = new SKBitmap(info);
        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);
        canvas.SetMatrix(matrix);
        using var image = SKImage.FromBitmap(source);
        canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest), null);
        return result;
    }

    /// <summary>Center-crops to a square and resizes to <paramref name="targetSize"/> (up or down).</summary>
    private static SKBitmap CropAndResize(SKBitmap source, int targetSize)
    {
        var side = Math.Min(source.Width, source.Height);
        var cropRect = SKRect.Create((source.Width - side) / 2, (source.Height - side) / 2, side, side);
        var destRect = SKRect.Create(0, 0, targetSize, targetSize);

        // Mipmapped linear filtering averages many source pixels when shrinking;
        // a cubic (Mitchell) resampler gives smoother results when enlarging.
        var sampling = side > targetSize
            ? new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear)
            : new SKSamplingOptions(SKCubicResampler.Mitchell);

        var result = new SKBitmap(new SKImageInfo(targetSize, targetSize, SKColorType.Rgba8888, source.AlphaType));
        using var image = SKImage.FromBitmap(source);
        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawImage(image, cropRect, destRect, sampling, null);
        return result;
    }

    private sealed class UnknownImageFormatException : Exception;

    private sealed class InvalidImageContentException : Exception;
}
