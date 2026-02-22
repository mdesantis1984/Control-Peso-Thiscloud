using ControlPeso.Application.Interfaces;
using ControlPeso.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ControlPeso.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de preferencias de usuario
/// </summary>
internal sealed class UserPreferencesService : IUserPreferencesService
{
    private readonly ControlPesoDbContext _context;
    private readonly ILogger<UserPreferencesService> _logger;

    public UserPreferencesService(
        ControlPesoDbContext context,
        ILogger<UserPreferencesService> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);

        _context = context;
        _logger = logger;
    }

    public async Task<bool> GetDarkModePreferenceAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var userIdStr = userId.ToString();

            var preferences = await _context.UserPreferences
                .AsNoTracking()
                .Where(p => p.UserId == userIdStr)
                .Select(p => p.DarkMode)
                .FirstOrDefaultAsync(ct);

            // Si no existe preferencia, retornar true (dark mode por defecto)
            var isDarkMode = preferences == 1;

            _logger.LogDebug(
                "UserPreferencesService: Dark mode preference retrieved - UserId: {UserId}, IsDarkMode: {IsDarkMode}",
                userId, isDarkMode);

            return isDarkMode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "UserPreferencesService: Error retrieving dark mode preference for user {UserId}",
                userId);

            // En caso de error, retornar dark mode por defecto
            return true;
        }
    }

    public async Task UpdateDarkModeAsync(Guid userId, bool isDarkMode, CancellationToken ct = default)
    {
        try
        {
            var userIdStr = userId.ToString();

            var preferences = await _context.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == userIdStr, ct);

            if (preferences == null)
            {
                // Si no existen preferencias, crearlas con el valor especificado
                await CreateDefaultPreferencesAsync(userId, ct);

                // Obtener las preferencias recién creadas para actualizarlas
                preferences = await _context.UserPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userIdStr, ct);

                if (preferences == null)
                {
                    _logger.LogWarning(
                        "UserPreferencesService: Failed to create default preferences for user {UserId}",
                        userId);
                    return;
                }
            }

            // Actualizar el valor de DarkMode
            preferences.DarkMode = isDarkMode ? 1 : 0;
            preferences.UpdatedAt = DateTime.UtcNow.ToString("O");

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "UserPreferencesService: Dark mode preference updated - UserId: {UserId}, IsDarkMode: {IsDarkMode}",
                userId, isDarkMode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "UserPreferencesService: Error updating dark mode preference for user {UserId}",
                userId);
            throw;
        }
    }

    public async Task CreateDefaultPreferencesAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var userIdStr = userId.ToString();

            // Verificar si ya existen preferencias
            var exists = await _context.UserPreferences
                .AnyAsync(p => p.UserId == userIdStr, ct);

            if (exists)
            {
                _logger.LogDebug(
                    "UserPreferencesService: Preferences already exist for user {UserId}",
                    userId);
                return;
            }

            // Crear preferencias por defecto
            var preferences = new Domain.Entities.UserPreferences
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userIdStr,
                DarkMode = 1, // Dark mode por defecto
                NotificationsEnabled = 1, // Notificaciones habilitadas por defecto
                TimeZone = "America/Argentina/Buenos_Aires", // Zona horaria por defecto
                UpdatedAt = DateTime.UtcNow.ToString("O")
            };

            _context.UserPreferences.Add(preferences);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "UserPreferencesService: Default preferences created for user {UserId}",
                userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "UserPreferencesService: Error creating default preferences for user {UserId}",
                userId);
            throw;
        }
    }
}
