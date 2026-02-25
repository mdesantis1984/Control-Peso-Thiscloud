namespace ControlPeso.Application.Interfaces;

/// <summary>
/// Service for processing and optimizing images (resize, crop, compress, format conversion).
/// Used for avatar photo optimization before storage.
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Processes an uploaded image for use as an avatar:
    /// - Crops to square aspect ratio (1:1)
    /// - Resizes to standard dimensions (512x512px)
    /// - Converts to WebP format for optimal compression
    /// - Applies quality compression (maintains visual quality)
    /// </summary>
    /// <param name="inputStream">Original image stream (JPEG, PNG, etc.)</param>
    /// <param name="outputStream">Output stream for processed WebP image</param>
    /// <param name="targetSize">Target width/height in pixels (default: 512)</param>
    /// <param name="quality">WebP quality 1-100 (default: 85, balances size/quality)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    /// <exception cref="ArgumentNullException">If inputStream or outputStream is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">If targetSize or quality is out of valid range</exception>
    /// <exception cref="InvalidOperationException">If image processing fails</exception>
    Task ProcessAvatarImageAsync(
        Stream inputStream,
        Stream outputStream,
        int targetSize = 512,
        int quality = 85,
        CancellationToken ct = default);

    /// <summary>
    /// Validates if a stream contains a valid image format (JPEG, PNG, WebP, etc.).
    /// </summary>
    /// <param name="stream">Image stream to validate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if valid image, false otherwise</returns>
    Task<bool> IsValidImageAsync(Stream stream, CancellationToken ct = default);

    /// <summary>
    /// Gets the detected format of an image stream (JPEG, PNG, WebP, etc.).
    /// </summary>
    /// <param name="stream">Image stream</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Image format name (e.g., "Jpeg", "Png", "WebP") or null if invalid</returns>
    Task<string?> GetImageFormatAsync(Stream stream, CancellationToken ct = default);
}
