using MudBlazor;
using MudBlazor.Utilities;

namespace ControlPeso.Web.Theme;

/// <summary>
/// Tema oscuro personalizado para Control Peso Thiscloud
/// Basado en las referencias visuales proporcionadas
/// </summary>
public static class ControlPesoTheme
{
    public static MudTheme DarkTheme => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#2196F3",          // Azul principal (botones, acciones)
            Secondary = "#424242",        // Gris oscuro
            AppbarBackground = "#1E1E1E", // Background del header
            Background = "#121212",       // Background principal
            Surface = "#1E1E1E",          // Background de cards/surfaces
            DrawerBackground = "#1E1E1E", // Background del sidebar
            DrawerText = "#FFFFFF",       // Texto del sidebar
            DrawerIcon = "#FFFFFF",       // Íconos del sidebar
            TextPrimary = "#FFFFFF",      // Texto principal
            TextSecondary = "#B0BEC5",    // Texto secundario
            ActionDefault = "#B0BEC5",    // Acciones default
            ActionDisabled = "#616161",   // Acciones deshabilitadas
            Divider = "#424242",          // Divisores
            Success = "#4CAF50",          // Verde (éxito, tendencia Down)
            Error = "#F44336",            // Rojo (error, tendencia Up)
            Warning = "#FF9800",          // Naranja (advertencias)
            Info = "#2196F3",             // Azul info
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#2196F3",          // Azul principal
            Secondary = "#424242",        // Gris oscuro
            AppbarBackground = "#1E1E1E", // Background del header
            Background = "#121212",       // Background principal
            Surface = "#1E1E1E",          // Background de cards/surfaces
            DrawerBackground = "#1E1E1E", // Background del sidebar
            DrawerText = "#FFFFFF",       // Texto del sidebar
            DrawerIcon = "#FFFFFF",       // Íconos del sidebar
            TextPrimary = "#FFFFFF",      // Texto principal
            TextSecondary = "#B0BEC5",    // Texto secundario
            ActionDefault = "#B0BEC5",    // Acciones default
            ActionDisabled = "#616161",   // Acciones deshabilitadas
            Divider = "#424242",          // Divisores
            Success = "#4CAF50",          // Verde (éxito, tendencia Down)
            Error = "#F44336",            // Rojo (error, tendencia Up)
            Warning = "#FF9800",          // Naranja (advertencias)
            Info = "#2196F3",             // Azul info
        },
        LayoutProperties = new LayoutProperties
        {
            DrawerWidthLeft = "240px",
            AppbarHeight = "64px",
        },
        ZIndex = new ZIndex
        {
            Drawer = 1100,
            AppBar = 1200,
            Dialog = 1300,
        }
    };
}
