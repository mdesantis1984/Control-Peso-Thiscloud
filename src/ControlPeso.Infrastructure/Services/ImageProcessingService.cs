using ControlPeso.Application.Interfaces;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace ControlPeso.Infrastructure.Services;

/// <summary>
/// Image processing service using SixLabors.ImageSharp for avatar optimization.
/// Handles resize, crop, format conversion, and compression.
/// </summary>
internal sealed class ImageProcessingService : IImageProcessingService
{
    private readonly ILogger<ImageProcessingService> _logger;

    private static readonly HashSet<string> SupportedFormats =
    [
        "Jpeg", "Png", "Gif", "Bmp", "WebP", "Tiff"
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
            // Load image from input stream (auto-detects format)
            using var image = await Image.LoadAsync(inputStream, ct);

            _logger.LogDebug(
                "Image loaded successfully - OriginalSize: {Width}x{Height}, Format: {Format}",
                image.Width, image.Height, image.Metadata.DecodedImageFormat?.Name ?? "Unknown");

            // Validate minimum dimensions
            if (image.Width < 32 || image.Height < 32)
            {
                throw new InvalidOperationException(
                    $"Image too small (min 32x32px). Actual: {image.Width}x{image.Height}");
            }

            // Process image: crop to square + resize
            image.Mutate(ctx =>
            {
                // 1. Crop to square aspect ratio (center crop)
                var minDimension = Math.Min(image.Width, image.Height);
                var cropRect = new Rectangle(
                    x: (image.Width - minDimension) / 2,
                    y: (image.Height - minDimension) / 2,
                    width: minDimension,
                    height: minDimension
                );

                ctx.Crop(cropRect);

                // 2. Resize to target size (high-quality Lanczos3 resampler)
                if (minDimension != targetSize)
                {
                    ctx.Resize(new ResizeOptions
                    {
                        Size = new Size(targetSize, targetSize),
                        Mode = ResizeMode.Max,
                        Sampler = KnownResamplers.Lanczos3,
                        Compand = true // Apply gamma correction for better quality
                    });
                }
            });

            _logger.LogDebug(
                "Image processed successfully - FinalSize: {Width}x{Height}",
                image.Width, image.Height);

            // 3. Save as WebP with specified quality
            var webpEncoder = new WebpEncoder
            {
                Quality = quality,
                Method = WebpEncodingMethod.BestQuality, // Slower but better compression
                FileFormat = WebpFileFormatType.Lossy,
                NearLossless = false
            };

            await image.SaveAsync(outputStream, webpEncoder, ct);

            _logger.LogInformation(
                "Avatar image processed successfully - Size: {Size}px, Quality: {Quality}, OutputFormat: WebP",
                targetSize, quality);
        }
        catch (UnknownImageFormatException ex)
        {
            _logger.LogError(ex, "Invalid image format - unable to decode");
            throw new InvalidOperationException("Invalid image format. Supported: JPEG, PNG, GIF, BMP, WebP, TIFF", ex);
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

            // Try to identify image format without fully loading the image
            var format = await Image.DetectFormatAsync(stream, ct);

            if (format == null)
            {
                _logger.LogWarning("Unable to detect image format - invalid image");
                return false;
            }

            var formatName = format.Name;
            var isSupported = SupportedFormats.Contains(formatName);

            _logger.LogDebug(
                "Image format detected: {Format}, Supported: {IsSupported}",
                formatName, isSupported);

            return isSupported;
        }
        catch (UnknownImageFormatException ex)
        {
            _logger.LogWarning(ex, "Unknown or invalid image format");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating image stream");
            return false;
        }
        finally
        {
            // Reset stream position for subsequent reads
            if (stream.CanSeek)
            {
                stream.Position = originalPosition;
            }
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
            var format = await Image.DetectFormatAsync(stream, ct);

            return format?.Name;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting image format");
            return null;
        }
        finally
        {
            // Reset stream position
            if (stream.CanSeek)
            {
                stream.Position = originalPosition;
            }
        }
    }
}
