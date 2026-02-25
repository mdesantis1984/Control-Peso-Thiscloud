namespace ControlPeso.Domain.Enums;

/// <summary>
/// Severity level for user notifications
/// Maps to INTEGER in database (UserNotifications.Type column)
/// Also maps to MudBlazor.Severity in Web layer for UI display
/// </summary>
public enum NotificationSeverity
{
    /// <summary>Normal notification</summary>
    Normal = 0,

    /// <summary>Informational notification</summary>
    Info = 1,

    /// <summary>Success notification</summary>
    Success = 2,

    /// <summary>Warning notification</summary>
    Warning = 3,

    /// <summary>Error notification (always shown, even if notifications disabled)</summary>
    Error = 4
}
