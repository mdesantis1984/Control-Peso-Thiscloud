using System.Security.Claims;
using ControlPeso.Application.Interfaces;
using ControlPeso.Web.Services.Storage;
using Microsoft.AspNetCore.Components.Authorization;

namespace ControlPeso.Web.Services;

/// <summary>
/// Servicio para gestionar la persistencia de la preferencia de tema del usuario.
/// Prioridad: 1) Base de datos (si usuario autenticado), 2) localStorage (fallback para usuarios no autenticados)
/// </summary>
public sealed class ThemeService
{
    private readonly IStorageService _storageService;
    private readonly IUserPreferencesService _userPreferencesService;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<ThemeService> _logger;
    private const string StorageKey = "IsDarkMode";

    public ThemeService(
        IStorageService storageService,
        IUserPreferencesService userPreferencesService,
        AuthenticationStateProvider authStateProvider,
        ILogger<ThemeService> logger)
    {
        ArgumentNullException.ThrowIfNull(storageService);
        ArgumentNullException.ThrowIfNull(userPreferencesService);
        ArgumentNullException.ThrowIfNull(authStateProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _storageService = storageService;
        _userPreferencesService = userPreferencesService;
        _authStateProvider = authStateProvider;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene la preferencia de tema guardada del usuario.
    /// Prioridad: 1) Base de datos (si autenticado), 2) localStorage (si no autenticado)
    /// </summary>
    /// <returns>True si el usuario prefiere modo oscuro, False para modo claro</returns>
    public async Task<bool> GetUserThemePreferenceAsync()
    {
        try
        {
            // 1. Intentar obtener de la base de datos si el usuario está autenticado
            var authState = await _authStateProvider.GetAuthenticationStateAsync();

            if (authState.User.Identity?.IsAuthenticated ?? false)
            {
                var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrWhiteSpace(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                {
                    var themeFromDb = await _userPreferencesService.GetDarkModePreferenceAsync(userId);

                    _logger.LogDebug(
                        "ThemeService: Theme preference loaded from database - UserId: {UserId}, IsDarkMode: {IsDarkMode}",
                        userId, themeFromDb);

                    return themeFromDb;
                }
            }

            // 2. Fallback: intentar obtener de localStorage si no está autenticado
            var value = await _storageService.GetItemAsync(StorageKey);

            if (!string.IsNullOrWhiteSpace(value) && bool.TryParse(value, out bool themeFromStorage))
            {
                _logger.LogDebug(
                    "ThemeService: Theme preference loaded from storage (fallback) - IsDarkMode: {IsDarkMode}",
                    themeFromStorage);

                return themeFromStorage;
            }

            // Valor por defecto: modo oscuro (según diseño del proyecto)
            _logger.LogDebug("ThemeService: No theme preference found, using default (dark mode)");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ThemeService: Error reading theme preference, using default (dark mode)");
            return true; // Fallback a modo oscuro
        }
    }

    /// <summary>
    /// Guarda la preferencia de tema del usuario.
    /// Prioridad: 1) Base de datos (si autenticado), 2) localStorage (si no autenticado)
    /// </summary>
    /// <param name="isDarkMode">True para modo oscuro, False para modo claro</param>
    public async Task SetUserThemePreferenceAsync(bool isDarkMode)
    {
        try
        {
            // 1. Intentar guardar en la base de datos si el usuario está autenticado
            var authState = await _authStateProvider.GetAuthenticationStateAsync();

            if (authState.User.Identity?.IsAuthenticated ?? false)
            {
                var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrWhiteSpace(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                {
                    await _userPreferencesService.UpdateDarkModeAsync(userId, isDarkMode);

                    _logger.LogInformation(
                        "ThemeService: Theme preference saved to database - UserId: {UserId}, IsDarkMode: {IsDarkMode}",
                        userId, isDarkMode);

                    return; // Guardado exitoso en DB, no necesitamos localStorage
                }
            }

            // 2. Fallback: guardar en localStorage si no está autenticado
            await _storageService.SetItemAsync(StorageKey, isDarkMode.ToString());

            _logger.LogInformation(
                "ThemeService: Theme preference saved to storage (fallback) - IsDarkMode: {IsDarkMode}",
                isDarkMode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ThemeService: Error saving theme preference");
        }
    }
}

