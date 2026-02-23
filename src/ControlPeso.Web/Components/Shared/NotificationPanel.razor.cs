using System.Security.Claims;
using ControlPeso.Application.DTOs;
using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace ControlPeso.Web.Components.Shared;

/// <summary>
/// Panel popover que muestra el historial de notificaciones del usuario
/// </summary>
public partial class NotificationPanel : IDisposable
{
    [Parameter]
    public bool IsOpen { get; set; }
    
    [Parameter]
    public EventCallback<bool> IsOpenChanged { get; set; }
    
    [Parameter]
    public EventCallback<int> OnUnreadCountChanged { get; set; }
    
    [Inject] 
    private IUserNotificationService NotificationService { get; set; } = null!;
    
    [Inject] 
    private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    
    [Inject] 
    private ILogger<NotificationPanel> Logger { get; set; } = null!;
    
    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;
    
    private bool _isLoading = true;
    private List<UserNotificationDto> _notifications = [];
    private Guid? _lastUserId = null;

    protected override async Task OnInitializedAsync()
    {
        // Suscribirse a cambios de autenticación
        AuthStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (IsOpen)
        {
            var currentUserId = await GetUserIdAsync();

            // Recargar si:
            // - Primera carga (_isLoading == true)
            // - Usuario cambió (_lastUserId != currentUserId)
            if (_isLoading || _lastUserId != currentUserId)
            {
                _lastUserId = currentUserId;
                await LoadNotificationsAsync();
            }
        }
    }

    private async void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        Logger.LogInformation("NotificationPanel: Authentication state changed - resetting panel");
        _notifications = [];
        _isLoading = true;
        _lastUserId = null;
        await InvokeAsync(StateHasChanged);
    }
    
    private async Task LoadNotificationsAsync()
    {
        _isLoading = true;

        try
        {
            var userId = await GetUserIdAsync();
            if (userId.HasValue)
            {
                // Load ALL notifications (read + unread) with large page size
                var result = await NotificationService.GetAllAsync(userId.Value, page: 1, pageSize: 50);
                _notifications = result.Items.ToList();

                Logger.LogInformation(
                    "NotificationPanel: Loaded {Count} notifications for user {UserId}",
                    _notifications.Count, userId.Value);
            }
            else
            {
                _notifications = [];
                Logger.LogWarning("NotificationPanel: Cannot load notifications - user not authenticated");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "NotificationPanel: Error loading notifications");
            Snackbar.Add("Error al cargar notificaciones", Severity.Error);
            _notifications = [];
        }
        finally
        {
            _isLoading = false;
        }
    }
    
    private Color GetSeverityColor(NotificationSeverity severity) => severity switch
    {
        NotificationSeverity.Normal => Color.Default,
        NotificationSeverity.Info => Color.Info,
        NotificationSeverity.Success => Color.Success,
        NotificationSeverity.Warning => Color.Warning,
        NotificationSeverity.Error => Color.Error,
        _ => Color.Default
    };
    
    private string GetSeverityLabel(NotificationSeverity severity) => severity switch
    {
        NotificationSeverity.Normal => "Normal",
        NotificationSeverity.Info => "Info",
        NotificationSeverity.Success => "Éxito",
        NotificationSeverity.Warning => "Advertencia",
        NotificationSeverity.Error => "Error",
        _ => "Normal"
    };
    
    private string GetNotificationClass(UserNotificationDto notification)
    {
        return notification.IsRead 
            ? "notification-read mud-theme-transparent" 
            : "notification-unread mud-theme-primary-lighten";
    }
    
    private string FormatTimestamp(DateTime createdAt)
    {
        var now = DateTime.UtcNow;
        var diff = now - createdAt;
        
        if (diff.TotalMinutes < 1)
            return "Hace un momento";
        if (diff.TotalMinutes < 60)
            return $"Hace {(int)diff.TotalMinutes} min";
        if (diff.TotalHours < 24)
            return $"Hace {(int)diff.TotalHours}h";
        if (diff.TotalDays < 7)
            return $"Hace {(int)diff.TotalDays}d";
        
        return createdAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }
    
    private async Task DeleteAsync(Guid notificationId)
    {
        try
        {
            await NotificationService.DeleteAsync(notificationId);
            _notifications.RemoveAll(n => n.Id == notificationId);
            
            Logger.LogInformation("NotificationPanel: Deleted notification {NotificationId}", notificationId);
            
            // Notificar cambio de contador
            await NotifyUnreadCountChangedAsync();
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "NotificationPanel: Error deleting notification {NotificationId}", notificationId);
            Snackbar.Add("Error al borrar notificación", Severity.Error);
        }
    }
    
    private async Task DeleteAllAsync()
    {
        var userId = await GetUserIdAsync();
        if (!userId.HasValue) return;
        
        try
        {
            await NotificationService.DeleteAllAsync(userId.Value);
            _notifications.Clear();
            
            Logger.LogInformation("NotificationPanel: Deleted all notifications for user {UserId}", userId.Value);
            
            // Notificar cambio de contador
            await NotifyUnreadCountChangedAsync();
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "NotificationPanel: Error deleting all notifications");
            Snackbar.Add("Error al borrar notificaciones", Severity.Error);
        }
    }
    
    private async Task MarkAllAsReadAsync()
    {
        var userId = await GetUserIdAsync();
        if (!userId.HasValue) return;
        
        try
        {
            await NotificationService.MarkAllAsReadAsync(userId.Value);
            
            foreach (var notification in _notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }
            
            Logger.LogInformation("NotificationPanel: Marked all notifications as read for user {UserId}", userId.Value);
            
            // Notificar cambio de contador
            await NotifyUnreadCountChangedAsync();
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "NotificationPanel: Error marking all as read");
            Snackbar.Add("Error al marcar notificaciones como leídas", Severity.Error);
        }
    }
    
    private async Task NotifyUnreadCountChangedAsync()
    {
        var unreadCount = _notifications.Count(n => !n.IsRead);
        
        if (OnUnreadCountChanged.HasDelegate)
        {
            await OnUnreadCountChanged.InvokeAsync(unreadCount);
        }
    }
    
    private async Task<Guid?> GetUserIdAsync()
    {
        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            
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
            Logger.LogError(ex, "NotificationPanel: Error getting user ID");
            return null;
        }
    }
    
    public void Dispose()
    {
        AuthStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        Logger.LogDebug("NotificationPanel: Disposed");
    }
}
