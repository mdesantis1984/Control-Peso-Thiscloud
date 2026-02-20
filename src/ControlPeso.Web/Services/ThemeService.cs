using Microsoft.AspNetCore.Http;

namespace ControlPeso.Web.Services;

/// <summary>
/// Servicio para gestionar la persistencia de la preferencia de tema del usuario
/// Utiliza cookies HTTP para evitar FOUC (Flash of Unstyled Content) en Blazor Server con prerenderizado
/// </summary>
public sealed class ThemeService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ThemeService> _logger;
    private const string CookieName = "IsDarkMode";

    public ThemeService(IHttpContextAccessor httpContextAccessor, ILogger<ThemeService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene la preferencia de tema guardada del usuario desde la cookie HTTP
    /// </summary>
    /// <returns>True si el usuario prefiere modo oscuro, False para modo claro</returns>
    public Task<bool> GetUserThemePreferenceAsync()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext != null && httpContext.Request.Cookies.TryGetValue(CookieName, out var value))
            {
                if (bool.TryParse(value, out bool isDarkMode))
                {
                    _logger.LogDebug("ThemeService: User theme preference loaded from cookie - IsDarkMode: {IsDarkMode}", isDarkMode);
                    return Task.FromResult(isDarkMode);
                }
            }

            // Valor por defecto: modo oscuro (según diseño del proyecto)
            _logger.LogDebug("ThemeService: No theme preference found, using default (dark mode)");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ThemeService: Error reading theme preference cookie, using default (dark mode)");
            return Task.FromResult(true); // Fallback a modo oscuro
        }
    }

    /// <summary>
    /// Guarda la preferencia de tema del usuario en una cookie HTTP persistente
    /// </summary>
    /// <param name="isDarkMode">True para modo oscuro, False para modo claro</param>
    public Task SetUserThemePreferenceAsync(bool isDarkMode)
    {
        try
        {
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

                _logger.LogInformation("ThemeService: User theme preference saved - IsDarkMode: {IsDarkMode}", isDarkMode);
            }
            else
            {
                _logger.LogWarning("ThemeService: HttpContext is null, cannot save theme preference");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ThemeService: Error saving theme preference cookie");
        }

        return Task.CompletedTask;
    }
}
