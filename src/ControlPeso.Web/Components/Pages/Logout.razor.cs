using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace ControlPeso.Web.Components.Pages;

/// <summary>
/// Página de confirmación de cierre de sesión.
/// Requiere autenticación ([Authorize]).
/// </summary>
[Authorize]
public partial class Logout
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    /// <summary>
    /// Confirma el logout y redirige al endpoint de cierre de sesión.
    /// </summary>
    private void ConfirmLogout()
    {
        // Navegar al endpoint GET /api/auth/logout
        NavigationManager.NavigateTo("/api/auth/logout?returnUrl=/login", forceLoad: true);
    }

    /// <summary>
    /// Cancela el logout y vuelve a la página anterior.
    /// </summary>
    private void Cancel()
    {
        NavigationManager.NavigateTo("/");
    }
}
