using Microsoft.AspNetCore.Components;

namespace ControlPeso.Web.Components.Pages;

/// <summary>
/// Página de autenticación OAuth con Google y LinkedIn.
/// </summary>
public partial class Login
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    /// <summary>
    /// Inicia el flujo de autenticación con Google OAuth.
    /// Redirige al endpoint Challenge de ASP.NET Core Authentication.
    /// </summary>
    private void LoginWithGoogle()
    {
        // Endpoint OAuth Challenge para Google
        // El middleware de autenticación manejará el redirect a Google OAuth
        var returnUrl = NavigationManager.ToAbsoluteUri("/").ToString();
        NavigationManager.NavigateTo(
            $"/api/auth/login/google?returnUrl={Uri.EscapeDataString(returnUrl)}",
            forceLoad: true);
    }

    /// <summary>
    /// Inicia el flujo de autenticación con LinkedIn OAuth.
    /// Redirige al endpoint Challenge de ASP.NET Core Authentication.
    /// </summary>
    private void LoginWithLinkedIn()
    {
        // Endpoint OAuth Challenge para LinkedIn
        // El middleware de autenticación manejará el redirect a LinkedIn OAuth
        var returnUrl = NavigationManager.ToAbsoluteUri("/").ToString();
        NavigationManager.NavigateTo(
            $"/api/auth/login/linkedin?returnUrl={Uri.EscapeDataString(returnUrl)}",
            forceLoad: true);
    }
}
