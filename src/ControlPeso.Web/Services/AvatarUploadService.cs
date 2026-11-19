using System.Text.RegularExpressions;
using ControlPeso.Application.Interfaces;

namespace ControlPeso.Web.Services;

/// <summary>Why an avatar upload was refused.</summary>
public enum AvatarRejectionReason
{
    /// <summary>No image data was supplied.</summary>
    Empty,

    /// <summary>The image exceeds <see cref="AvatarUploadService.MaxImageBytes"/>.</summary>
    TooLarge,

    /// <summary>The payload is not valid base64.</summary>
    InvalidEncoding,

    /// <summary>The bytes are not a decodable, supported image.</summary>
    InvalidImage
}

/// <summary>Thrown when the uploaded avatar data is not acceptable. Nothing is written to disk.</summary>
public sealed class AvatarRejectedException(AvatarRejectionReason reason)
    : Exception($"Avatar upload rejected: {reason}")
{
    public AvatarRejectionReason Reason { get; } = reason;
}

/// <summary>Validates, re-encodes and stores user avatars, and removes the ones they replace.</summary>
public interface IAvatarUploadService
{
    /// <summary>
    /// Validates the base64 image (optionally a <c>data:</c> URL) by content, re-encodes it to a square WebP
    /// and stores it under a server-generated name.
    /// </summary>
    /// <returns>The public URL, <c>/uploads/avatars/&lt;guid&gt;.webp</c>.</returns>
    /// <exception cref="AvatarRejectedException">Empty, oversized, badly encoded or non-image input.</exception>
    Task<string> SaveAsync(string? encodedImage, CancellationToken ct = default);

    /// <summary>
    /// Deletes a previously stored avatar. Only <c>/uploads/avatars/&lt;name&gt;.webp</c> URLs that resolve to a file
    /// inside the avatars folder are touched; external URLs and anything else are ignored.
    /// </summary>
    /// <returns><c>true</c> when a file was deleted.</returns>
    bool TryDeleteLocalAvatar(string? avatarUrl);
}

public sealed partial class AvatarUploadService : IAvatarUploadService
{
    /// <summary>Largest accepted image once decoded from base64 (1 MB; the cropper sends a 256x256 image).</summary>
    public const int MaxImageBytes = 1024 * 1024;

    /// <summary>Largest base64 payload (without a data URL prefix) that can decode to <see cref="MaxImageBytes"/>.</summary>
    public const int MaxEncodedLength = (MaxImageBytes + 2) / 3 * 4;

    /// <summary>
    /// SignalR hub receive limit: the biggest accepted payload plus room for the data URL prefix
    /// and the message envelope.
    /// </summary>
    public const int MaxHubMessageBytes = 2 * 1024 * 1024;

    private const int TargetSize = 256; // matches the cropper output, so nothing is upscaled
    private const int Quality = 85;
    private const string UrlPrefix = "/uploads/avatars/";

    [GeneratedRegex(@"^/uploads/avatars/[A-Za-z0-9_\-.]+\.webp\z", RegexOptions.CultureInvariant)]
    private static partial Regex LocalAvatarUrlRegex();

    private readonly IImageProcessingService _imageProcessing;
    private readonly string _avatarsPath;
    private readonly ILogger<AvatarUploadService> _logger;

    public AvatarUploadService(
        IImageProcessingService imageProcessing,
        IWebHostEnvironment environment,
        ILogger<AvatarUploadService> logger)
    {
        ArgumentNullException.ThrowIfNull(imageProcessing);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(logger);

        _imageProcessing = imageProcessing;
        _logger = logger;
        _avatarsPath = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads", "avatars"));
    }

    public async Task<string> SaveAsync(string? encodedImage, CancellationToken ct = default)
    {
        var payload = ExtractPayload(encodedImage);
        if (payload.IsEmpty)
        {
            throw new AvatarRejectedException(AvatarRejectionReason.Empty);
        }

        // Size is checked on the encoded text, before allocating or decoding anything.
        if (payload.Length > MaxEncodedLength)
        {
            throw new AvatarRejectedException(AvatarRejectionReason.TooLarge);
        }

        var buffer = new byte[payload.Length / 4 * 3 + 3];
        if (!Convert.TryFromBase64Chars(payload, buffer, out var written))
        {
            throw new AvatarRejectedException(AvatarRejectionReason.InvalidEncoding);
        }

        if (written == 0)
        {
            throw new AvatarRejectedException(AvatarRejectionReason.Empty);
        }

        if (written > MaxImageBytes)
        {
            throw new AvatarRejectedException(AvatarRejectionReason.TooLarge);
        }

        // Content-based validation and re-encode: the original bytes are never stored.
        using var output = new MemoryStream();
        try
        {
            using var input = new MemoryStream(buffer, 0, written, writable: false);
            await _imageProcessing.ProcessAvatarImageAsync(input, output, TargetSize, Quality, ct);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Avatar upload rejected: content is not a valid image");
            throw new AvatarRejectedException(AvatarRejectionReason.InvalidImage);
        }

        Directory.CreateDirectory(_avatarsPath);

        var fileName = $"{Guid.NewGuid()}.webp";
        var filePath = Path.Combine(_avatarsPath, fileName);
        await File.WriteAllBytesAsync(filePath, output.ToArray(), ct);

        _logger.LogInformation("Avatar stored - File: {FileName}, Size: {Size} bytes", fileName, output.Length);
        return UrlPrefix + fileName;
    }

    public bool TryDeleteLocalAvatar(string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl) || !LocalAvatarUrlRegex().IsMatch(avatarUrl))
        {
            return false;
        }

        var fileName = avatarUrl[UrlPrefix.Length..];
        var fullPath = Path.GetFullPath(Path.Combine(_avatarsPath, fileName));

        // Defense in depth: the resolved file must be a direct child of the avatars folder.
        if (!string.Equals(Path.GetDirectoryName(fullPath), _avatarsPath, StringComparison.Ordinal)
            || !string.Equals(Path.GetFileName(fullPath), fileName, StringComparison.Ordinal))
        {
            _logger.LogWarning("Refusing to delete avatar outside the avatars folder: {AvatarUrl}", avatarUrl);
            return false;
        }

        try
        {
            if (!File.Exists(fullPath))
            {
                return false;
            }

            File.Delete(fullPath);
            _logger.LogDebug("Old avatar deleted - File: {FileName}", fileName);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not delete old avatar - File: {FileName}", fileName);
            return false;
        }
    }

    /// <summary>Returns the base64 text of a raw payload or a <c>data:image/...;base64,</c> URL, trimmed.</summary>
    private static ReadOnlySpan<char> ExtractPayload(string? encodedImage)
    {
        if (string.IsNullOrWhiteSpace(encodedImage))
        {
            return default;
        }

        var span = encodedImage.AsSpan().Trim();
        if (span.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = span.IndexOf(',');
            return comma < 0 ? span : span[(comma + 1)..].Trim();
        }

        return span;
    }
}
