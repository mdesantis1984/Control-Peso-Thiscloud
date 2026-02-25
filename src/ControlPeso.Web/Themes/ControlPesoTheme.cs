using MudBlazor;

namespace ControlPeso.Web.Themes;

/// <summary>
/// Tema personalizado para Control Peso Thiscloud
/// Basado en el prototipo Google AI Studio con Material Design
/// Soporta modo claro y oscuro manteniendo consistencia visual
/// </summary>
public static class ControlPesoTheme
{
    /// <summary>
    /// Tema principal de la aplicación (Dark Mode)
    /// Colores, spacing, typography y componentes alineados al prototipo
    /// </summary>
    public static readonly MudTheme DarkTheme = new()
    {
        PaletteLight = new PaletteLight
        {
            // Primary: Material Blue 500 (#2196F3)
            Primary = "#2196F3",
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken = "#1976D2",
            PrimaryLighten = "#42A5F5",

            // Secondary: Blue Grey 200 (#B0BEC5)
            Secondary = "#B0BEC5",
            SecondaryContrastText = "#000000",
            SecondaryDarken = "#90A4AE",
            SecondaryLighten = "#CFD8DC",

            // Tertiary: Material Teal 500 (#009688)
            Tertiary = "#009688",
            TertiaryContrastText = "#FFFFFF",

            // Info, Success, Warning, Error (Material Design)
            Info = "#2196F3",
            InfoContrastText = "#FFFFFF",
            Success = "#4CAF50",
            SuccessContrastText = "#FFFFFF",
            Warning = "#FF9800",
            WarningContrastText = "#FFFFFF",
            Error = "#F44336",
            ErrorContrastText = "#FFFFFF",

            // Dark (usado para textos oscuros en light mode)
            Dark = "#212121",
            DarkContrastText = "#FFFFFF",
            DarkDarken = "#000000",
            DarkLighten = "#424242",

            // Background y Surface (Light Mode)
            Background = "#FAFAFA",
            BackgroundGray = "#F5F5F5",
            Surface = "#FFFFFF",

            // Texto (Light Mode)
            TextPrimary = "rgba(0, 0, 0, 0.87)",
            TextSecondary = "rgba(0, 0, 0, 0.60)",
            TextDisabled = "rgba(0, 0, 0, 0.38)",

            // Action (Light Mode)
            ActionDefault = "rgba(0, 0, 0, 0.54)",
            ActionDisabled = "rgba(0, 0, 0, 0.26)",
            ActionDisabledBackground = "rgba(0, 0, 0, 0.12)",

            // Divider (Light Mode)
            Divider = "rgba(0, 0, 0, 0.12)",
            DividerLight = "rgba(0, 0, 0, 0.08)",

            // Table
            TableLines = "rgba(0, 0, 0, 0.12)",
            TableStriped = "rgba(0, 0, 0, 0.02)",
            TableHover = "rgba(0, 0, 0, 0.04)",

            // AppBar
            AppbarBackground = "#FFFFFF",
            AppbarText = "rgba(0, 0, 0, 0.87)",

            // Drawer
            DrawerBackground = "#FFFFFF",
            DrawerText = "rgba(0, 0, 0, 0.87)",
            DrawerIcon = "rgba(0, 0, 0, 0.54)",

            // Overlay
            OverlayLight = "rgba(255, 255, 255, 0.4980)",
            OverlayDark = "rgba(33, 33, 33, 0.4980)"
        },

        PaletteDark = new PaletteDark
        {
            // Primary: Material Blue 500 (#2196F3) - IGUAL en dark mode
            Primary = "#2196F3",
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken = "#1976D2",
            PrimaryLighten = "#42A5F5",

            // Secondary: Blue Grey 200 (#B0BEC5) - IGUAL en dark mode
            Secondary = "#B0BEC5",
            SecondaryContrastText = "#000000",
            SecondaryDarken = "#90A4AE",
            SecondaryLighten = "#CFD8DC",

            // Tertiary: Material Teal 500 (#009688) - IGUAL en dark mode
            Tertiary = "#009688",
            TertiaryContrastText = "#FFFFFF",

            // Info, Success, Warning, Error (Material Design) - IGUALES
            Info = "#2196F3",
            InfoContrastText = "#FFFFFF",
            Success = "#4CAF50",
            SuccessContrastText = "#FFFFFF",
            Warning = "#FF9800",
            WarningContrastText = "#FFFFFF",
            Error = "#F44336",
            ErrorContrastText = "#FFFFFF",

            // Dark (para componentes oscuros)
            Dark = "#FAFAFA",
            DarkContrastText = "#000000",
            DarkDarken = "#E0E0E0",
            DarkLighten = "#FFFFFF",

            // Background y Surface (Dark Mode - WeightTracker color scheme)
            // WeightTracker usa: Background #0B1622, Surface #1B2838, AppBar #0D1B2A
            Background = "#0B1622", // Fondo muy oscuro (casi negro con tinte azul)
            BackgroundGray = "#101820", // Fondo alternativo
            Surface = "#1B2838", // Cards background (gris azulado oscuro)

            // Texto (Dark Mode)
            TextPrimary = "rgba(255, 255, 255, 0.95)", // Texto principal casi blanco
            TextSecondary = "rgba(255, 255, 255, 0.60)", // Texto secundario
            TextDisabled = "rgba(255, 255, 255, 0.38)",

            // Action (Dark Mode)
            ActionDefault = "rgba(255, 255, 255, 0.70)",
            ActionDisabled = "rgba(255, 255, 255, 0.30)",
            ActionDisabledBackground = "rgba(255, 255, 255, 0.12)",

            // Divider (Dark Mode - más sutil según prototipo)
            Divider = "rgba(255, 255, 255, 0.08)", // Más sutil que default 0.12
            DividerLight = "rgba(255, 255, 255, 0.05)",

            // Table (Dark Mode - más sutil)
            TableLines = "rgba(255, 255, 255, 0.08)",
            TableStriped = "rgba(255, 255, 255, 0.02)",
            TableHover = "rgba(255, 255, 255, 0.04)",

            // AppBar (Dark Mode)
            AppbarBackground = "#0D1B2A", // AppBar ligeramente más claro que background
            AppbarText = "rgba(255, 255, 255, 0.95)",

            // Drawer (Dark Mode)
            DrawerBackground = "#0D1B2A", // Sidebar oscuro
            DrawerText = "rgba(255, 255, 255, 0.95)",
            DrawerIcon = "rgba(255, 255, 255, 0.70)",

            // Overlay
            OverlayLight = "rgba(11, 22, 34, 0.7)",
            OverlayDark = "rgba(11, 22, 34, 0.9)"
        },

        LayoutProperties = new LayoutProperties
        {
            // Default Layout Spacing (8pt grid)
            DefaultBorderRadius = "12px", // Material Design standard para cards (aumentado de 4px → 8px → 12px)
            DrawerWidthLeft = "280px", // Default MudBlazor
            DrawerWidthRight = "280px",
            AppbarHeight = "64px" // Default MudBlazor
        },

        Shadows = new Shadow
        {
            // Elevations más sutiles según prototipo (Material Design con reducción)
            // Prototipo usa sombras menos pronunciadas
            // MudBlazor requiere al menos 26 elementos (índices 0-25)
            Elevation = new[]
            {
                "none", // 0
                "0 1px 3px 0 rgba(0, 0, 0, 0.12), 0 1px 2px 0 rgba(0, 0, 0, 0.08)", // 1 (muy sutil)
                "0 2px 4px 0 rgba(0, 0, 0, 0.12), 0 2px 3px 0 rgba(0, 0, 0, 0.08)", // 2 (cards)
                "0 3px 6px 0 rgba(0, 0, 0, 0.12), 0 3px 5px 0 rgba(0, 0, 0, 0.08)", // 3
                "0 4px 8px 0 rgba(0, 0, 0, 0.12), 0 4px 6px 0 rgba(0, 0, 0, 0.08)", // 4 (AppBar)
                "0 6px 10px 0 rgba(0, 0, 0, 0.12), 0 6px 8px 0 rgba(0, 0, 0, 0.08)", // 5
                "0 8px 12px 0 rgba(0, 0, 0, 0.12), 0 8px 10px 0 rgba(0, 0, 0, 0.08)", // 6 (FAB)
                "0 9px 14px 0 rgba(0, 0, 0, 0.12), 0 9px 12px 0 rgba(0, 0, 0, 0.08)", // 7
                "0 10px 16px 0 rgba(0, 0, 0, 0.12), 0 10px 14px 0 rgba(0, 0, 0, 0.08)", // 8 (Drawer)
                "0 12px 18px 0 rgba(0, 0, 0, 0.12), 0 12px 16px 0 rgba(0, 0, 0, 0.08)", // 9
                "0 14px 20px 0 rgba(0, 0, 0, 0.12), 0 14px 18px 0 rgba(0, 0, 0, 0.08)", // 10
                "0 16px 24px 0 rgba(0, 0, 0, 0.12), 0 16px 20px 0 rgba(0, 0, 0, 0.08)", // 11
                "0 18px 28px 0 rgba(0, 0, 0, 0.12), 0 18px 22px 0 rgba(0, 0, 0, 0.08)", // 12
                "0 20px 32px 0 rgba(0, 0, 0, 0.12), 0 20px 24px 0 rgba(0, 0, 0, 0.08)", // 13
                "0 22px 36px 0 rgba(0, 0, 0, 0.12), 0 22px 26px 0 rgba(0, 0, 0, 0.08)", // 14
                "0 24px 40px 0 rgba(0, 0, 0, 0.12), 0 24px 28px 0 rgba(0, 0, 0, 0.08)", // 15
                "0 26px 44px 0 rgba(0, 0, 0, 0.12), 0 26px 30px 0 rgba(0, 0, 0, 0.08)", // 16
                "0 28px 48px 0 rgba(0, 0, 0, 0.12), 0 28px 32px 0 rgba(0, 0, 0, 0.08)", // 17
                "0 30px 52px 0 rgba(0, 0, 0, 0.12), 0 30px 34px 0 rgba(0, 0, 0, 0.08)", // 18
                "0 32px 56px 0 rgba(0, 0, 0, 0.12), 0 32px 36px 0 rgba(0, 0, 0, 0.08)", // 19
                "0 34px 60px 0 rgba(0, 0, 0, 0.12), 0 34px 38px 0 rgba(0, 0, 0, 0.08)", // 20
                "0 36px 64px 0 rgba(0, 0, 0, 0.12), 0 36px 40px 0 rgba(0, 0, 0, 0.08)", // 21
                "0 38px 68px 0 rgba(0, 0, 0, 0.12), 0 38px 42px 0 rgba(0, 0, 0, 0.08)", // 22
                "0 40px 72px 0 rgba(0, 0, 0, 0.12), 0 40px 44px 0 rgba(0, 0, 0, 0.08)", // 23
                "0 42px 76px 0 rgba(0, 0, 0, 0.12), 0 42px 46px 0 rgba(0, 0, 0, 0.08)", // 24
                "0 44px 80px 0 rgba(0, 0, 0, 0.12), 0 44px 48px 0 rgba(0, 0, 0, 0.08)"  // 25 (extra para evitar IndexOutOfRangeException)
            }
        },

        ZIndex = new ZIndex
        {
            Drawer = 1200,
            AppBar = 1100,
            Dialog = 1300,
            Popover = 1400,
            Snackbar = 1500,
            Tooltip = 1600
        }
    };

    /// <summary>
    /// Sistema de espaciado basado en 8pt grid (Material Design)
    /// </summary>
    public static class Spacing
    {
        public const int XS = 4;   // 4px  - Spacing mínimo
        public const int SM = 8;   // 8px  - Spacing pequeño (base)
        public const int MD = 16;  // 16px - Spacing medio (2 * base)
        public const int LG = 24;  // 24px - Spacing grande (3 * base)
        public const int XL = 32;  // 32px - Spacing extra grande (4 * base)
        public const int XXL = 48; // 48px - Spacing masivo (6 * base)
    }

    /// <summary>
    /// Breakpoints de Material Design (para referencia)
    /// MudBlazor los maneja automáticamente en MudGrid
    /// </summary>
    public static class Breakpoints
    {
        public const int XS = 0;    // 0-599px   (Mobile)
        public const int SM = 600;  // 600-959px (Tablet portrait)
        public const int MD = 960;  // 960-1279px (Tablet landscape / small desktop)
        public const int LG = 1280; // 1280-1919px (Desktop)
        public const int XL = 1920; // 1920px+ (Large desktop)
    }
}
