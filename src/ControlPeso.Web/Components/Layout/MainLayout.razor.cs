using Microsoft.AspNetCore.Components;

namespace ControlPeso.Web.Components.Layout;

/// <summary>
/// MainLayout - Layout principal de la aplicación con MudBlazor
/// Proporciona la estructura base con AppBar, Drawer y contenido principal
/// </summary>
public partial class MainLayout
{
    [Inject] private ILogger<MainLayout> Logger { get; set; } = null!;

    private bool _drawerOpen = true;
    private bool _isDarkMode = true;

    protected override void OnInitialized()
    {
        Logger.LogInformation("MainLayout: Initializing");
        base.OnInitialized();
    }

    private void ToggleDrawer()
    {
        _drawerOpen = !_drawerOpen;
        Logger.LogDebug("MainLayout: Drawer toggled - Open: {IsOpen}", _drawerOpen);
    }

    private async Task ToggleDarkMode()
    {
        _isDarkMode = !_isDarkMode;
        Logger.LogInformation("MainLayout: Dark mode toggled - DarkMode: {IsDarkMode}", _isDarkMode);

        // TODO: P5.8 - Guardar preferencia en UserPreferences cuando se implemente
        // await UserPreferencesService.UpdateDarkModeAsync(_isDarkMode);

        StateHasChanged();
    }

    private void DismissError()
    {
        Logger.LogInformation("MainLayout: Blazor error UI dismissed");
        // La lógica de dismissal es manejada por el framework Blazor
    }
}
