using Microsoft.AspNetCore.Components;

namespace ControlPeso.Web.Components.Shared;

/// <summary>
/// NotificationBell - Componente de notificaciones (implementación temporal para P5.1)
/// Será completado en P5.9 con funcionalidad completa
/// </summary>
public partial class NotificationBell
{
    [Inject] private ILogger<NotificationBell> Logger { get; set; } = null!;

    private int _notificationCount = 0;

    private void OpenNotifications()
    {
        Logger.LogInformation("NotificationBell: Notifications clicked");
        // TODO: P5.9 - Implementar apertura de panel de notificaciones
    }
}
