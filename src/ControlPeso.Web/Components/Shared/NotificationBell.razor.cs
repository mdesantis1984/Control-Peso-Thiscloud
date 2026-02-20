using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ControlPeso.Web.Components.Shared;

/// <summary>
/// NotificationBell - Componente de notificaciones (implementación temporal para P5.1)
/// Será completado en P5.9 con funcionalidad completa
/// </summary>
public partial class NotificationBell
{
    [Inject] private ILogger<NotificationBell> Logger { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private int _notificationCount = 0;

    private void OpenNotifications()
    {
        Logger.LogInformation("NotificationBell: Notifications clicked");

        // Mostrar mensaje temporal mientras se implementa P5.9
        Snackbar.Add("Las notificaciones se implementarán en la siguiente fase", Severity.Info);
    }
}
