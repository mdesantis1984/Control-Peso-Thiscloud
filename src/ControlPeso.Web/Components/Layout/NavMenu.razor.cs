using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace ControlPeso.Web.Components.Layout;

/// <summary>
/// NavMenu - Menú de navegación principal con MudNavMenu
/// Muestra diferentes opciones según el estado de autenticación y rol del usuario
/// </summary>
public partial class NavMenu
{
    [Inject] private ILogger<NavMenu> Logger { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    private bool _isAdmin = false;

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("NavMenu: Initializing");

        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            _isAdmin = authState.User.IsInRole("Administrator");

            Logger.LogDebug("NavMenu: User is admin: {IsAdmin}", _isAdmin);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "NavMenu: Error checking user role");
        }
    }
}
