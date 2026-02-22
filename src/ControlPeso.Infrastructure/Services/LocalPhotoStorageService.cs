using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ControlPeso.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ControlPeso.Infrastructure.Services;

/// <summary>
/// Local file system implementation of photo storage service.
/// Stores photos in wwwroot/uploads/avatars with normalized, sanitized filenames.
/// Integrates ImageProcessingService for automatic optimization (resize, crop, WebP conversion).
/// Prepared for Docker volume mounting and Windows file systems.
/// </summary>
internal sealed partial class LocalPhotoStorageService : IPhotoStorageService
{
    private readonly ILogger<LocalPhotoStorageService> _logger;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly string _uploadsPath;
    private readonly string _relativeUrlPath;

    // Regex para remover caracteres inválidos (compiled para performance)
    [GeneratedRegex(@"[^\w\-\.]", RegexOptions.Compiled)]
    private static partial Regex InvalidCharsRegex();

    public LocalPhotoStorageService(
        IConfiguration configuration,
        IImageProcessingService imageProcessingService,
        ILogger<LocalPhotoStorageService> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(imageProcessingService);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _imageProcessingService = imageProcessingService;

        // Leer configuración o usar defaults
        var webRootPath = configuration["PhotoStorage:WebRootPath"] ?? "wwwroot";
        var uploadsFolder = configuration["PhotoStorage:AvatarsFolder"] ?? "uploads/avatars";

        _uploadsPath = Path.Combine(webRootPath, uploadsFolder);
        _relativeUrlPath = $"/{uploadsFolder.Replace("\\", "/")}";

        // Crear directorio si no existe
        if (!Directory.Exists(_uploadsPath))
        {
            Directory.CreateDirectory(_uploadsPath);
            _logger.LogInformation("Created avatars directory at {Path}", _uploadsPath);
        }

        _logger.LogDebug("PhotoStorageService initialized - UploadsPath: {UploadsPath}, RelativeUrl: {RelativeUrl}",
            _uploadsPath, _relativeUrlPath);
    }

    public async Task<string> SaveUserPhotoAsync(Guid userId, string fileName, Stream stream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty", nameof(fileName));

        _logger.LogInformation("Saving photo for user {UserId} - OriginalFileName: {FileName}",
            userId, fileName);

        try
        {
            // 1. Eliminar fotos anteriores del usuario
            var deletedCount = await DeleteUserPhotosAsync(userId, ct);
            if (deletedCount > 0)
            {
                _logger.LogDebug("Deleted {Count} existing photo(s) for user {UserId}", deletedCount, userId);
            }

            // 2. Procesar imagen (crop, resize, WebP conversion)
            _logger.LogDebug("Processing image for user {UserId}", userId);

            using var processedStream = new MemoryStream();
            await _imageProcessingService.ProcessAvatarImageAsync(
                inputStream: stream,
                outputStream: processedStream,
                targetSize: 512,
                quality: 85,
                ct: ct);

            processedStream.Position = 0; // Reset stream position for reading

            var processedSizeKB = (processedStream.Length / 1024.0).ToString("F2", CultureInfo.InvariantCulture);
            _logger.LogInformation(
                "Image processed successfully - OriginalSize: {OriginalSize} bytes, ProcessedSize: {ProcessedSizeKB} KB",
                stream.Length, processedSizeKB);

            // 3. Normalizar y sanitizar nombre de archivo
            var sanitizedName = SanitizeFileName(Path.GetFileNameWithoutExtension(fileName));

            // 4. Generar nombre único: {userId}_{timestamp}_{sanitized}.webp (siempre WebP después de procesar)
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var finalFileName = $"{userId}_{timestamp}_{sanitizedName}.webp";

            // 5. Guardar archivo procesado
            var filePath = Path.Combine(_uploadsPath, finalFileName);

            await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await processedStream.CopyToAsync(fileStream, ct);
            }

            // 6. Generar URL relativa
            var relativeUrl = $"{_relativeUrlPath}/{finalFileName}";

            _logger.LogInformation(
                "Avatar saved successfully - UserId: {UserId}, Path: {RelativeUrl}, FinalSize: {ProcessedSizeKB} KB (WebP)",
                userId, relativeUrl, processedSizeKB);

            return relativeUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving photo for user {UserId} - FileName: {FileName}",
                userId, fileName);
            throw;
        }
    }

    public Task<int> DeleteUserPhotosAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        _logger.LogDebug("Deleting photos for user {UserId}", userId);

        try
        {
            // Buscar todos los archivos que empiecen con el userId
            var pattern = $"{userId}_*";
            var files = Directory.GetFiles(_uploadsPath, pattern);

            foreach (var file in files)
            {
                File.Delete(file);
                _logger.LogDebug("Deleted photo: {FilePath}", file);
            }

            _logger.LogInformation("Deleted {Count} photo(s) for user {UserId}", files.Length, userId);

            return Task.FromResult(files.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photos for user {UserId}", userId);
            throw;
        }
    }

    public bool PhotoExists(string photoUrl)
    {
        if (string.IsNullOrWhiteSpace(photoUrl))
            return false;

        try
        {
            // Remover query string si existe (cache buster)
            var urlWithoutQuery = photoUrl.Split('?')[0];

            // Convertir URL relativa a path físico
            var fileName = Path.GetFileName(urlWithoutQuery);
            var filePath = Path.Combine(_uploadsPath, fileName);

            return File.Exists(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking photo existence for URL: {PhotoUrl}", photoUrl);
            return false;
        }
    }

    /// <summary>
    /// Normaliza y sanitiza un nombre de archivo:
    /// - Convierte a lowercase
    /// - Remueve acentos (á→a, é→e, etc.)
    /// - Reemplaza espacios por guiones bajos
    /// - Remueve caracteres inválidos (solo permite: a-z, 0-9, _, -, .)
    /// - Limita a 50 caracteres
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "photo";

        // 1. Convertir a lowercase
        var normalized = fileName.ToLowerInvariant();

        // 2. Remover acentos usando normalización Unicode
        normalized = RemoveDiacritics(normalized);

        // 3. Reemplazar espacios por guiones bajos
        normalized = normalized.Replace(' ', '_');

        // 4. Remover caracteres inválidos (solo permite: a-z, 0-9, _, -, .)
        normalized = InvalidCharsRegex().Replace(normalized, string.Empty);

        // 5. Remover guiones bajos o guiones consecutivos
        normalized = Regex.Replace(normalized, @"[_\-]{2,}", "_");

        // 6. Limitar longitud a 50 caracteres
        if (normalized.Length > 50)
            normalized = normalized[..50];

        // 7. Remover guiones bajos o guiones al inicio/final
        normalized = normalized.Trim('_', '-');

        // 8. Fallback si quedó vacío
        if (string.IsNullOrWhiteSpace(normalized))
            return "photo";

        return normalized;
    }

    /// <summary>
    /// Remueve diacríticos (acentos) de un string.
    /// Ejemplos: á→a, é→e, í→i, ó→o, ú→u, ñ→n, ü→u, ç→c
    /// </summary>
    private static string RemoveDiacritics(string text)
    {
        // Normalizar a FormD (descomponer caracteres acentuados)
        var normalizedString = text.Normalize(NormalizationForm.FormD);

        var stringBuilder = new StringBuilder();

        // Filtrar solo caracteres que NO sean marcas diacríticas
        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        // Re-normalizar a FormC (componer caracteres)
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}
