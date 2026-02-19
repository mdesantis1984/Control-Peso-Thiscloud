using MudBlazor;

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

    /// <summary>
    /// Sistema de tipografía (Material Design Typography Scale)
    /// MudBlazor usa Typo.* enums que mapean a estos tamaños
    /// </summary>
    /// <remarks>
    /// Typo.h1 → 3rem (48px) / weight 300
    /// Typo.h2 → 2.5rem (40px) / weight 300
    /// Typo.h3 → 2rem (32px) / weight 400
    /// Typo.h4 → 1.75rem (28px) / weight 500 (Page Titles)
    /// Typo.h5 → 1.5rem (24px) / weight 400 (AppBar Title)
    /// Typo.h6 → 1.25rem (20px) / weight 500 (Card Headers)
    /// Typo.body1 → 1rem (16px) / weight 400 (Default text)
    /// Typo.body2 → 0.875rem (14px) / weight 400 (Secondary text)
    /// Typo.button → 0.875rem (14px) / weight 500
    /// Typo.caption → 0.75rem (12px) / weight 400 (Small text)
    /// Typo.subtitle1 → 1rem (16px) / weight 400
    /// Typo.subtitle2 → 0.875rem (14px) / weight 500
    /// Typo.overline → 0.75rem (12px) / weight 400 / uppercase
    /// </remarks>
    public static class Typography
    {
        // Estos valores son los defaults de MudBlazor y coinciden con Material Design
        // Se documentan aquí para referencia del equipo de desarrollo
    }

    /// <summary>
    /// Sistema de espaciados basado en 8pt grid (múltiplos de 8px)
    /// Usar con MudBlazor spacing classes: pa-*, ma-*, mt-*, mb-*, ml-*, mr-*, px-*, py-*, mx-*, my-*
    /// </summary>
    public static class Spacing
    {
        /// <summary>XS: 4px (Class="pa-1")</summary>
        public const int XS = 4;

        /// <summary>SM: 8px (Class="pa-2")</summary>
        public const int SM = 8;

        /// <summary>MD: 16px (Class="pa-4") - Spacing default para cards, forms</summary>
        public const int MD = 16;

        /// <summary>LG: 24px (Class="pa-6") - Spacing para secciones, headers</summary>
        public const int LG = 24;

        /// <summary>XL: 32px (Class="pa-8") - Spacing para grandes contenedores</summary>
        public const int XL = 32;

        /// <summary>XXL: 48px (Class="pa-12") - Spacing para separación de secciones principales</summary>
        public const int XXL = 48;
    }

    /// <summary>
    /// Breakpoints responsivos (MudBlazor built-in)
    /// Usar con MudGrid: xs="12" sm="6" md="4" lg="3" xl="2"
    /// </summary>
    public static class Breakpoints
    {
        /// <summary>XS: 0-599px (Mobile portrait)</summary>
        public const int XS = 0;

        /// <summary>SM: 600-959px (Mobile landscape, Tablet portrait)</summary>
        public const int SM = 600;

        /// <summary>MD: 960-1279px (Tablet landscape, Small desktop)</summary>
        public const int MD = 960;

        /// <summary>LG: 1280-1919px (Desktop)</summary>
        public const int LG = 1280;

        /// <summary>XL: 1920px+ (Large desktop, 4K)</summary>
        public const int XL = 1920;
    }
}
