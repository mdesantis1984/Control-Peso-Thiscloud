using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ControlPeso.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de preferencias de usuario
/// </summary>
public sealed class UserPreferencesService : IUserPreferencesService
{
    private readonly IDbContextFactory<ControlPesoDbContext> _contextFactory;
    private readonly ILogger<UserPreferencesService> _logger;

    public UserPreferencesService(
        IDbContextFactory<ControlPesoDbContext> contextFactory,
        ILogger<UserPreferencesService> logger)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<bool> GetDarkModePreferenceAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("🔍 GetDarkModePreferenceAsync - START - UserId: {UserId}", userId);

            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var preferences = await context.UserPreferences
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .FirstOrDefaultAsync(ct);

            if (preferences == null)
            {
                _logger.LogWarning("⚠️ NO PREFERENCES FOUND - Creating defaults - UserId: {UserId}", userId);
                await CreateDefaultPreferencesAsync(userId, ct);
                return true; // Dark mode por defecto
            }

            var isDarkMode = preferences.DarkMode;

            _logger.LogInformation(
                "✅ DarkMode retrieved - UserId: {UserId}, Result: {Result}",
                userId, isDarkMode);

            return isDarkMode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ ERROR retrieving dark mode preference for user {UserId}",
                userId);

            // En caso de error, retornar dark mode por defecto
            return true;
        }
    }

    public async Task<bool> GetNotificationsEnabledAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("🔍 GetNotificationsEnabledAsync - START - UserId: {UserId}", userId);

            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var preferences = await context.UserPreferences
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .FirstOrDefaultAsync(ct);

            if (preferences == null)
            {
                _logger.LogWarning("⚠️ NO PREFERENCES FOUND - Creating defaults - UserId: {UserId}", userId);
                await CreateDefaultPreferencesAsync(userId, ct);
                return true; // Notificaciones habilitadas por defecto
            }

            var isEnabled = preferences.NotificationsEnabled;

            _logger.LogInformation(
                "✅ NotificationsEnabled retrieved - UserId: {UserId}, Result: {Result}",
                userId, isEnabled);

            return isEnabled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ ERROR retrieving notifications preference for user {UserId}",
                userId);

            // En caso de error, retornar habilitadas por defecto
            return true;
        }
    }

    public async Task UpdateDarkModeAsync(Guid userId, bool isDarkMode, CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var preferences = await context.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (preferences == null)
            {
                // Si no existen preferencias, crearlas con el valor especificado
                await CreateDefaultPreferencesAsync(userId, ct);

                // Obtener las preferencias recién creadas para actualizarlas
                preferences = await context.UserPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId, ct);

                if (preferences == null)
                {
                    _logger.LogWarning(
                        "UserPreferencesService: Failed to create default preferences for user {UserId}",
                        userId);
                    return;
                }
            }

            // Actualizar el valor de DarkMode
            preferences.DarkMode = isDarkMode;
            preferences.UpdatedAt = DateTime.UtcNow;

            // Forzar EF a actualizar todos los campos
            context.Entry(preferences).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

            await context.SaveChangesAsync(ct);

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

    public async Task UpdateNotificationsEnabledAsync(Guid userId, bool isEnabled, CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var preferences = await context.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (preferences == null)
            {
                // Si no existen preferencias, crearlas con el valor especificado
                await CreateDefaultPreferencesAsync(userId, ct);

                // Obtener las preferencias recién creadas para actualizarlas
                preferences = await context.UserPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId, ct);

                if (preferences == null)
                {
                    _logger.LogWarning(
                        "UserPreferencesService: Failed to create default preferences for user {UserId}",
                        userId);
                    return;
                }
            }

            // Actualizar el valor de NotificationsEnabled
            preferences.NotificationsEnabled = isEnabled;
            preferences.UpdatedAt = DateTime.UtcNow;

            // Forzar EF a actualizar todos los campos
            context.Entry(preferences).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

            await context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "UserPreferencesService: Notifications preference updated - UserId: {UserId}, IsEnabled: {IsEnabled}",
                userId, isEnabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "UserPreferencesService: Error updating notifications preference for user {UserId}",
                userId);
            throw;
        }
    }

    public async Task UpdatePreferencesAsync(Guid userId, bool isDarkMode, bool notificationsEnabled, CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var preferences = await context.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (preferences == null)
            {
                // Si no existen preferencias, crearlas
                await CreateDefaultPreferencesAsync(userId, ct);

                // Obtener las preferencias recién creadas para actualizarlas
                preferences = await context.UserPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId, ct);

                if (preferences == null)
                {
                    _logger.LogWarning(
                        "UserPreferencesService: Failed to create default preferences for user {UserId}",
                        userId);
                    return;
                }
            }

            // Actualizar ambos valores
            preferences.DarkMode = isDarkMode;
            preferences.NotificationsEnabled = notificationsEnabled;
            preferences.UpdatedAt = DateTime.UtcNow;

            // Forzar EF a actualizar todos los campos (no solo UpdatedAt)
            context.Entry(preferences).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

            await context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "UserPreferencesService: Preferences updated - UserId: {UserId}, IsDarkMode: {IsDarkMode}, NotificationsEnabled: {NotificationsEnabled}",
                userId, isDarkMode, notificationsEnabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "UserPreferencesService: Error updating preferences for user {UserId}",
                userId);
            throw;
        }
    }

    public async Task CreateDefaultPreferencesAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            // Verificar si ya existen preferencias
            var exists = await context.UserPreferences
                .AnyAsync(p => p.UserId == userId, ct);

            if (exists)
            {
                _logger.LogDebug(
                    "UserPreferencesService: Preferences already exist for user {UserId}",
                    userId);
                return;
            }

            // Crear preferencias por defecto
            var preferences = new UserPreferences
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DarkMode = true, // Dark mode por defecto
                NotificationsEnabled = true, // Notificaciones habilitadas por defecto
                TimeZone = "America/Argentina/Buenos_Aires", // Zona horaria por defecto
                UpdatedAt = DateTime.UtcNow
            };

            context.UserPreferences.Add(preferences);
            await context.SaveChangesAsync(ct);

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
