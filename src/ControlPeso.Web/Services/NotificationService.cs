using System.Security.Claims;
using ControlPeso.Application.DTOs;
using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Enums;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace ControlPeso.Web.Services;

/// <summary>
/// Servicio wrapper para ISnackbar que respeta las preferencias de notificaciones del usuario.
/// Solo muestra Snackbar si el usuario tiene notificaciones habilitadas.
/// Excepciones: siempre muestra notificaciones de tipo Error (críticas).
/// Además, guarda todas las notificaciones en el historial para usuarios autenticados.
/// </summary>
public sealed class NotificationService
{
    private readonly ISnackbar _snackbar;
    private readonly IUserPreferencesService _userPreferencesService;
    private readonly IUserNotificationService _userNotificationService;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ISnackbar snackbar,
        IUserPreferencesService userPreferencesService,
        IUserNotificationService userNotificationService,
        AuthenticationStateProvider authStateProvider,
        ILogger<NotificationService> logger)
    {
        ArgumentNullException.ThrowIfNull(snackbar);
        ArgumentNullException.ThrowIfNull(userPreferencesService);
        ArgumentNullException.ThrowIfNull(userNotificationService);
        ArgumentNullException.ThrowIfNull(authStateProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _snackbar = snackbar;
        _userPreferencesService = userPreferencesService;
        _userNotificationService = userNotificationService;
        _authStateProvider = authStateProvider;
        _logger = logger;
    }

    /// <summary>
    /// Muestra un Snackbar si el usuario tiene notificaciones habilitadas.
    /// Siempre muestra notificaciones de tipo Error (críticas).
    /// Guarda la notificación en el historial para usuarios autenticados.
    /// </summary>
    /// <param name="message">Mensaje a mostrar</param>
    /// <param name="severity">Severidad de la notificación</param>
    /// <param name="configure">Configuración adicional del Snackbar</param>
    public async Task<Snackbar?> AddAsync(
        string message,
        Severity severity = Severity.Normal,
        Action<SnackbarOptions>? configure = null)
    {
        Snackbar? snackbar = null;
        Guid? userId = null;

        // SIEMPRE mostrar notificaciones de ERROR (críticas)
        if (severity == Severity.Error)
        {
            _logger.LogDebug("NotificationService: Showing ERROR notification (always shown) - Message: '{Message}'", message);
            snackbar = _snackbar.Add(message, severity, configure);
            userId = await GetAuthenticatedUserIdAsync();
        }
        else
        {
            // Para otros tipos de notificaciones, verificar preferencia del usuario
            try
            {
                userId = await GetAuthenticatedUserIdAsync();

                if (userId.HasValue)
                {
                    var notificationsEnabled = await _userPreferencesService.GetNotificationsEnabledAsync(userId.Value);

                    if (!notificationsEnabled)
                    {
                        _logger.LogDebug(
                            "NotificationService: Notifications disabled for user {UserId} - Suppressing {Severity} notification: '{Message}'",
                            userId.Value, severity, message);
                        // No mostrar Snackbar, pero SÍ guardar en historial (ver abajo)
                    }
                    else
                    {
                        _logger.LogDebug(
                            "NotificationService: Showing {Severity} notification for user {UserId} - Message: '{Message}'",
                            severity, userId.Value, message);
                        snackbar = _snackbar.Add(message, severity, configure);
                    }
                }
                else
                {
                    // Usuario no autenticado → mostrar notificación (no hay preferencias)
                    _logger.LogDebug("NotificationService: User not authenticated - Showing notification: '{Message}'", message);
                    snackbar = _snackbar.Add(message, severity, configure);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationService: Error checking notification preferences - Showing notification anyway");
                snackbar = _snackbar.Add(message, severity, configure);
            }
        }

        // Guardar notificación en historial (solo para usuarios autenticados)
        if (userId.HasValue)
        {
            await SaveNotificationToHistoryAsync(userId.Value, message, severity);
        }

        return snackbar;
    }

    /// <summary>
    /// Guarda una notificación en el historial del usuario.
    /// No lanza excepciones si falla (log error pero no interrumpir flujo).
    /// </summary>
    private async Task SaveNotificationToHistoryAsync(Guid userId, string message, Severity severity)
    {
        try
        {
            // Convert MudBlazor.Severity to Domain.NotificationSeverity
            var notificationSeverity = ConvertToNotificationSeverity(severity);

            await _userNotificationService.CreateAsync(new CreateUserNotificationDto
            {
                UserId = userId,
                Type = notificationSeverity,
                Title = null, // Podría extraerse de SnackbarOptions si se necesita
                Message = message
            });

            _logger.LogDebug(
                "NotificationService: Saved notification to history - UserId: {UserId}, Type: {Type}",
                userId, notificationSeverity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "NotificationService: Error saving notification to history - UserId: {UserId}, Message: '{Message}'",
                userId, message);
            // No lanzar excepción - el historial es secundario
        }
    }

    /// <summary>
    /// Convert MudBlazor.Severity to Domain.NotificationSeverity
    /// </summary>
    private static NotificationSeverity ConvertToNotificationSeverity(Severity severity) => severity switch
    {
        Severity.Normal => NotificationSeverity.Normal,
        Severity.Info => NotificationSeverity.Info,
        Severity.Success => NotificationSeverity.Success,
        Severity.Warning => NotificationSeverity.Warning,
        Severity.Error => NotificationSeverity.Error,
        _ => NotificationSeverity.Normal
    };

    /// <summary>
    /// Obtiene el ID del usuario autenticado actual.
    /// </summary>
    private async Task<Guid?> GetAuthenticatedUserIdAsync()
    {
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();

            if (authState.User.Identity?.IsAuthenticated ?? false)
            {
                var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrWhiteSpace(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                {
                    return userId;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NotificationService: Error getting authenticated user ID");
            return null;
        }
    }

    /// <summary>
    /// Versión síncrona (fire-and-forget) para compatibilidad con código existente.
    /// Internamente llama a la versión async.
    /// </summary>
    public Snackbar? Add(string message, Severity severity = Severity.Normal, Action<SnackbarOptions>? configure = null)
    {
        // Fire-and-forget: no esperar el resultado
        _ = AddAsync(message, severity, configure);
        
        // Retornar null porque no podemos esperar el Task aquí
        // (el código que llama no espera un Task)
        return null;
    }

    /// <summary>
    /// Elimina un Snackbar específico.
    /// </summary>
    public void Remove(Snackbar snackbar)
    {
        _snackbar.Remove(snackbar);
    }

    /// <summary>
    /// Elimina todos los Snackbars visibles.
    /// </summary>
    public void RemoveAll()
    {
        _snackbar.Clear();
    }

    /// <summary>
    /// Configuración global del servicio Snackbar.
    /// </summary>
    public SnackbarConfiguration Configuration => _snackbar.Configuration;
}
