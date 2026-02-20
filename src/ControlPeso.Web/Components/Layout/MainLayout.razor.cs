using ControlPeso.Application.DTOs;
using ControlPeso.Application.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Security.Claims;
using ControlPeso.Web.Services;

namespace ControlPeso.Web.Components.Layout;

/// <summary>
/// MainLayout - Layout principal de la aplicación con MudBlazor
/// Proporciona la estructura base con AppBar, Drawer y contenido principal
/// </summary>
public partial class MainLayout
{
    [Inject] private ILogger<MainLayout> Logger { get; set; } = null!;
    [Inject] private IUserService UserService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private ThemeService ThemeService { get; set; } = null!;

    private bool _drawerOpen = true;
    private bool _isDarkMode = true; // Estado del tema - default dark mode
    private MudThemeProvider _themeProvider = null!; // Referencia al provider
    private UserDto? _currentUser;

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("MainLayout: Initializing");

        try
        {
            // Cargar preferencia de tema desde cookie
            _isDarkMode = await ThemeService.GetUserThemePreferenceAsync();
            Logger.LogInformation("MainLayout: Theme preference loaded - IsDarkMode: {IsDarkMode}", _isDarkMode);

            var authState = await AuthStateProvider.GetAuthenticationStateAsync();

            if (authState.User.Identity?.IsAuthenticated ?? false)
            {
                var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrWhiteSpace(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                {
                    _currentUser = await UserService.GetByIdAsync(userId);
                    Logger.LogInformation("MainLayout: User loaded - UserId: {UserId}, AvatarUrl: {AvatarUrl}", 
                        userId, _currentUser?.AvatarUrl ?? "(null)");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "MainLayout: Error loading current user");
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _themeProvider != null)
        {
            // Asegurar que el tema se aplica después del primer render
            await InvokeAsync(StateHasChanged);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private void ToggleDrawer()
    {
        _drawerOpen = !_drawerOpen;
        Logger.LogDebug("MainLayout: Drawer toggled - Open: {IsOpen}", _drawerOpen);
    }

    private async Task ToggleDarkModeAsync()
    {
        try
        {
            _isDarkMode = !_isDarkMode;

            // Guardar preferencia en cookie
            await ThemeService.SetUserThemePreferenceAsync(_isDarkMode);

            Logger.LogInformation("MainLayout: Dark mode toggled - IsDarkMode: {IsDarkMode}", _isDarkMode);

            // Forzar re-render
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "MainLayout: Error toggling dark mode");
        }
    }

    private void DismissError()
    {
        Logger.LogInformation("MainLayout: Blazor error UI dismissed");
        // La lógica de dismissal es manejada por el framework Blazor
    }
}
