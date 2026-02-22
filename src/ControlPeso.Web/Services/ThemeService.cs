using Microsoft.AspNetCore.Http;
using ControlPeso.Application.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace ControlPeso.Web.Services;

/// <summary>
/// Servicio para gestionar la persistencia de la preferencia de tema del usuario
/// Prioridad: 1) Base de datos (si usuario autenticado), 2) Cookies HTTP (fallback)
/// </summary>
public sealed class ThemeService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserPreferencesService _userPreferencesService;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<ThemeService> _logger;
    private const string CookieName = "IsDarkMode";

    public ThemeService(
        IHttpContextAccessor httpContextAccessor,
        IUserPreferencesService userPreferencesService,
        AuthenticationStateProvider authStateProvider,
        ILogger<ThemeService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(userPreferencesService);
        ArgumentNullException.ThrowIfNull(authStateProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _httpContextAccessor = httpContextAccessor;
        _userPreferencesService = userPreferencesService;
        _authStateProvider = authStateProvider;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene la preferencia de tema guardada del usuario
    /// Prioridad: 1) Base de datos (si autenticado), 2) Cookie HTTP (si no autenticado)
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
                    var isDarkMode = await _userPreferencesService.GetDarkModePreferenceAsync(userId);

                    _logger.LogDebug(
                        "ThemeService: Theme preference loaded from database - UserId: {UserId}, IsDarkMode: {IsDarkMode}",
                        userId, isDarkMode);

                    return isDarkMode;
                }
            }

            // 2. Fallback: intentar obtener de cookie si no está autenticado
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext != null && httpContext.Request.Cookies.TryGetValue(CookieName, out var value))
            {
                if (bool.TryParse(value, out bool isDarkMode))
                {
                    _logger.LogDebug(
                        "ThemeService: Theme preference loaded from cookie (fallback) - IsDarkMode: {IsDarkMode}",
                        isDarkMode);

                    return isDarkMode;
                }
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
    /// Guarda la preferencia de tema del usuario
    /// Prioridad: 1) Base de datos (si autenticado), 2) Cookie HTTP (si no autenticado)
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

                    return; // Guardado exitoso en DB, no necesitamos cookie
                }
            }

            // 2. Fallback: guardar en cookie si no está autenticado
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTimeOffset.MaxValue, // Cookie persistente (no expira)
                    HttpOnly = false, // Debe ser accesible desde JavaScript/Blazor
                    Secure = true, // Solo HTTPS
                    SameSite = SameSiteMode.Strict, // Protección CSRF
                    IsEssential = true // Esencial para funcionalidad
                };

                httpContext.Response.Cookies.Append(CookieName, isDarkMode.ToString(), cookieOptions);

                _logger.LogInformation(
                    "ThemeService: Theme preference saved to cookie (fallback) - IsDarkMode: {IsDarkMode}",
                    isDarkMode);
            }
            else
            {
                _logger.LogWarning("ThemeService: HttpContext is null, cannot save theme preference");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ThemeService: Error saving theme preference");
        }
    }
}

