using System.Security.Claims;
using ControlPeso.Application.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace ControlPeso.Web.Components.Shared;

/// <summary>
/// NotificationBell - Componente de campanita con badge de contador y panel de notificaciones
/// </summary>
public partial class NotificationBell : IDisposable
{
    [Inject] private ILogger<NotificationBell> Logger { get; set; } = null!;
    [Inject] private IUserNotificationService NotificationService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    [Parameter] public string? Class { get; set; }

    private int _unreadNotificationCount = 0;
    private bool _notificationPanelOpen = false;
    private System.Threading.Timer? _pollingTimer;

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("NotificationBell: Initializing");

        // Suscribirse a cambios de autenticación
        AuthStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;

        await LoadUnreadCountAsync();

        // Iniciar polling cada 60 segundos para actualizar contador
        _pollingTimer = new System.Threading.Timer(
            async _ => await LoadUnreadCountAsync(), 
            null, 
            TimeSpan.FromSeconds(60), 
            TimeSpan.FromSeconds(60));
    }

    private async void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        Logger.LogInformation("NotificationBell: Authentication state changed - reloading notifications");
        _unreadNotificationCount = 0;
        _notificationPanelOpen = false;
        await LoadUnreadCountAsync();
    }

    private void ToggleNotificationPanel()
    {
        _notificationPanelOpen = !_notificationPanelOpen;
        Logger.LogInformation("NotificationBell: Panel toggled - IsOpen: {IsOpen}", _notificationPanelOpen);
    }

    private async Task LoadUnreadCountAsync()
    {
        try
        {
            var userId = await GetUserIdAsync();

            if (userId.HasValue)
            {
                var count = await NotificationService.GetUnreadCountAsync(userId.Value);

                if (_unreadNotificationCount != count)
                {
                    _unreadNotificationCount = count;
                    Logger.LogDebug("NotificationBell: Unread count updated - Count: {Count}", count);
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "NotificationBell: Error loading unread count");
        }
    }

    private void UpdateUnreadCount(int count)
    {
        _unreadNotificationCount = count;
        Logger.LogDebug("NotificationBell: Unread count updated from panel - Count: {Count}", count);
        StateHasChanged();
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
            Logger.LogError(ex, "NotificationBell: Error getting user ID");
            return null;
        }
    }

    public void Dispose()
    {
        _pollingTimer?.Dispose();
        AuthStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        Logger.LogInformation("NotificationBell: Disposed");
    }
}
