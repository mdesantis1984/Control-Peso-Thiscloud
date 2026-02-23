using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;

namespace ControlPeso.Web.Components.Layout;

/// <summary>
/// NavMenu - Menú de navegación principal con MudNavMenu
/// Muestra diferentes opciones según el estado de autenticación y rol del usuario
/// </summary>
public partial class NavMenu
{
    [Inject] private ILogger<NavMenu> Logger { get; set; } = null!;
    [Inject] private IStringLocalizer<NavMenu> Localizer { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    private bool _isAdmin = false;

    // Localized Properties
    private string NavigationTitle => Localizer[nameof(NavigationTitle)];
    private string Dashboard => Localizer[nameof(Dashboard)];
    private string History => Localizer[nameof(History)];
    private string Trends => Localizer[nameof(Trends)];
    private string Profile => Localizer[nameof(Profile)];
    private string AdminSection => Localizer[nameof(AdminSection)];
    private string AdminPanel => Localizer[nameof(AdminPanel)];
    private string TelegramDiagnostics => Localizer[nameof(TelegramDiagnostics)];
    private string Home => Localizer[nameof(Home)];
    private string Login => Localizer[nameof(Login)];
    private string AppVersion => Localizer[nameof(AppVersion)];

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
