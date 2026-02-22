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
public partial class MainLayout : IDisposable
{
    [Inject] private ILogger<MainLayout> Logger { get; set; } = null!;
    [Inject] private IUserService UserService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private ThemeService ThemeService { get; set; } = null!;

    private bool _drawerOpen = true;
    private bool _isDarkMode = true; // Estado del tema - default dark mode
    private MudThemeProvider _themeProvider = null!; // Referencia al provider
    private UserDto? _currentUser;
    private long _cacheBuster = DateTime.UtcNow.Ticks; // Cache buster para avatar
    private bool _userMenuOpen = false; // Estado del menú de usuario

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("MainLayout: Initializing");

        // Suscribirse a cambios de autenticación
        AuthStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;

        try
        {
            // Cargar preferencia de tema desde cookie
            _isDarkMode = await ThemeService.GetUserThemePreferenceAsync();
            Logger.LogInformation("MainLayout: Theme preference loaded - IsDarkMode: {IsDarkMode}", _isDarkMode);

            // Cargar usuario actual si está autenticado
            await LoadCurrentUserAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "MainLayout: Error during initialization");
        }

        await base.OnInitializedAsync();
    }

    /// <summary>
    /// Handler para cambios en el estado de autenticación.
    /// Se ejecuta cuando el usuario hace login/logout.
    /// </summary>
    private async void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        try
        {
            Logger.LogInformation("MainLayout: Authentication state changed - reloading user");
            await LoadCurrentUserAsync();
            await InvokeAsync(StateHasChanged); // Forzar re-render
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "MainLayout: Error handling authentication state change");
        }
    }

    /// <summary>
    /// Carga el usuario actual desde el servicio si está autenticado.
    /// </summary>
    private async Task LoadCurrentUserAsync()
    {
        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            Logger.LogInformation("MainLayout: LoadCurrentUserAsync called - IsAuthenticated: {IsAuthenticated}, Identity.Name: {Name}",
                user.Identity?.IsAuthenticated ?? false,
                user.Identity?.Name ?? "(null)");

            // Log TODOS los claims para debug
            if (user.Identity?.IsAuthenticated ?? false)
            {
                Logger.LogInformation("MainLayout: User claims:");
                foreach (var claim in user.Claims)
                {
                    Logger.LogInformation("  - {Type}: {Value}", claim.Type, claim.Value);
                }
            }

            if (user.Identity?.IsAuthenticated ?? false)
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrWhiteSpace(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                {
                    Logger.LogInformation("MainLayout: Fetching user from database - UserId: {UserId}", userId);
                    _currentUser = await UserService.GetByIdAsync(userId);
                    _cacheBuster = DateTime.UtcNow.Ticks; // Refresh cache buster

                    if (_currentUser != null)
                    {
                        Logger.LogInformation("MainLayout: ✅ User loaded successfully - UserId: {UserId}, Name: {Name}, Email: {Email}, AvatarUrl: {AvatarUrl}", 
                            userId, _currentUser.Name, _currentUser.Email, _currentUser.AvatarUrl ?? "(null)");
                    }
                    else
                    {
                        Logger.LogWarning("MainLayout: ⚠️ UserService.GetByIdAsync returned null for UserId: {UserId}", userId);
                    }
                }
                else
                {
                    Logger.LogWarning("MainLayout: ⚠️ User authenticated but UserId claim invalid - Claim: {Claim}", 
                        userIdClaim ?? "(null)");
                    _currentUser = null;
                }
            }
            else
            {
                Logger.LogInformation("MainLayout: User not authenticated");
                _currentUser = null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "MainLayout: ❌ Error loading current user");
            _currentUser = null;
        }
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

    /// <summary>
    /// Dispose pattern para desuscribirse del evento.
    /// </summary>
    public void Dispose()
    {
        AuthStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}
