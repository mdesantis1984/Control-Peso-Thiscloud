using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ControlPeso.Web.Components.Layout;

/// <summary>
/// EmptyLayout - Layout vacío para páginas fullscreen sin navegación.
/// Usado en: Login, Error pages, Landing pages.
/// </summary>
public partial class EmptyLayout
{
    private bool _isDarkMode = true; // Dark mode por defecto
    private MudThemeProvider _themeProvider = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _themeProvider != null)
        {
            // Asegurar que el tema se aplica después del primer render
            await InvokeAsync(StateHasChanged);
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}
