namespace ControlPeso.Web.Pages.Legal;

public partial class Changelog
{
    private bool _isEnglish;

    private string PageTitleText => _isEnglish ? "Changelog - Control Peso Thiscloud" : "Historial de Cambios - Control Peso Thiscloud";
    private string MetaDescription => _isEnglish
        ? "View the complete version history and changelog for Control Peso Thiscloud. Track new features, bug fixes, and improvements across all releases."
        : "Vea el historial completo de versiones y cambios de Control Peso Thiscloud. Siga las nuevas funciones, correcciones de bugs y mejoras en todas las versiones.";

    private string Title => _isEnglish ? "Changelog" : "Historial de Cambios";
    private string LanguageSwitchTooltip => _isEnglish ? "Switch to Spanish" : "Cambiar a Inglés";

    private string IntroText => _isEnglish
        ? "All notable changes to Control Peso Thiscloud are documented here. This project adheres to Semantic Versioning."
        : "Todos los cambios notables de Control Peso Thiscloud se documentan aquí. Este proyecto se adhiere al Versionado Semántico.";

    private string UnreleasedLabel => _isEnglish ? "Unreleased" : "No Publicado";
    private string PlannedFeaturesTitle => _isEnglish ? "Planned Features" : "Funciones Planificadas";

    private string Feature1 => _isEnglish ? "Additional language support (Portuguese, French)" : "Soporte de idiomas adicionales (Portugués, Francés)";
    private string Feature2 => _isEnglish ? "Advanced analytics dashboard with custom date ranges" : "Panel de análisis avanzado con rangos de fechas personalizados";
    private string Feature3 => _isEnglish ? "Progressive Web App (PWA) support for offline access" : "Soporte de Progressive Web App (PWA) para acceso offline";
    private string Feature4 => _isEnglish ? "Email notifications for weight goals and milestones" : "Notificaciones por email para objetivos de peso e hitos";
    private string Feature5 => _isEnglish ? "Export data to CSV/Excel" : "Exportar datos a CSV/Excel";
    private string Feature6 => _isEnglish ? "Weight goal reminders and motivational messages" : "Recordatorios de objetivos de peso y mensajes motivacionales";

    private string CurrentVersionLabel => _isEnglish ? "Current Version" : "Versión Actual";
    private string InitialReleaseLabel => _isEnglish ? "Initial Release" : "Lanzamiento Inicial";

    private string AddedLabel => _isEnglish ? "Added" : "Agregado";
    private string CoreFeaturesLabel => _isEnglish ? "Core Features" : "Funciones Principales";

    // v1.3.0
    private string V130_Feature1 => _isEnglish
        ? "Production deployment infrastructure (Docker Compose, SQL Server, automated backups)"
        : "Infraestructura de despliegue en producción (Docker Compose, SQL Server, backups automáticos)";
    private string V130_Feature2 => _isEnglish
        ? "Application security hardening (HSTS, HTTPS redirection, CSP headers, forwarded headers)"
        : "Endurecimiento de seguridad de aplicación (HSTS, redirección HTTPS, headers CSP, forwarded headers)";
    private string V130_Feature3 => _isEnglish
        ? "SEO and accessibility improvements (Open Graph, Twitter Cards, semantic HTML)"
        : "Mejoras de SEO y accesibilidad (Open Graph, Twitter Cards, HTML semántico)";
    private string V130_Feature4 => _isEnglish
        ? "Legal documentation (Privacy Policy, Terms and Conditions, Third-Party Licenses, Changelog)"
        : "Documentación legal (Política de Privacidad, Términos y Condiciones, Licencias de Terceros, Historial de Cambios)";
    private string V130_Feature5 => _isEnglish
        ? "CI/CD automation with semantic versioning (automatic tag and release creation)"
        : "Automatización CI/CD con versionado semántico (creación automática de tags y releases)";

    // v1.2.0
    private string V120_Feature1 => _isEnglish
        ? "Theme management (MudBlazor ThemeManager integration, Dark/Light toggle, customizable colors)"
        : "Gestión de temas (integración MudBlazor ThemeManager, toggle Oscuro/Claro, colores personalizables)";
    private string V120_Feature2 => _isEnglish
        ? "Responsive design optimization (mobile-first layout, touch-friendly controls)"
        : "Optimización de diseño responsivo (layout mobile-first, controles táctiles)";
    private string V120_Feature3 => _isEnglish
        ? "Full bilingual support (Spanish/English with language selector)"
        : "Soporte bilingüe completo (Español/Inglés con selector de idioma)";

    // v1.1.0
    private string V110_Feature1 => _isEnglish
        ? "Trends and analytics (weight trend analysis, projections, statistical calculations)"
        : "Tendencias y análisis (análisis de tendencia de peso, proyecciones, cálculos estadísticos)";
    private string V110_Feature2 => _isEnglish
        ? "Advanced filtering (date range filters, search by notes, sorting, pagination)"
        : "Filtrado avanzado (filtros por rango de fechas, búsqueda por notas, ordenamiento, paginación)";
    private string V110_Feature3 => _isEnglish
        ? "Notes feature (add, edit, delete personal notes up to 500 characters)"
        : "Función de notas (agregar, editar, eliminar notas personales hasta 500 caracteres)";

    // v1.0.0
    private string V100_Feature1 => _isEnglish
        ? "Google OAuth 2.0 authentication (no password storage)"
        : "Autenticación Google OAuth 2.0 (sin almacenamiento de contraseñas)";
    private string V100_Feature2 => _isEnglish
        ? "Weight entry management (create, edit, delete weight logs)"
        : "Gestión de entradas de peso (crear, editar, eliminar logs de peso)";
    private string V100_Feature3 => _isEnglish
        ? "Weight history visualization (table and line chart)"
        : "Visualización de historial de peso (tabla y gráfico de líneas)";
    private string V100_Feature4 => _isEnglish
        ? "MudBlazor UI with dark theme by default"
        : "UI MudBlazor con tema oscuro por defecto";
    private string V100_Feature5 => _isEnglish
        ? "SQLite database with Entity Framework Core (Database First)"
        : "Base de datos SQLite con Entity Framework Core (Database First)";

    private string SemanticVersioningTitle => _isEnglish ? "Semantic Versioning" : "Versionado Semántico";
    private string SemanticVersioningText => _isEnglish
        ? "This project uses Semantic Versioning: MAJOR.MINOR.PATCH. MAJOR = incompatible API changes, MINOR = new features (backwards-compatible), PATCH = bug fixes (backwards-compatible)."
        : "Este proyecto usa Versionado Semántico: MAJOR.MINOR.PATCH. MAJOR = cambios incompatibles de API, MINOR = nuevas funciones (retrocompatibles), PATCH = correcciones de bugs (retrocompatibles).";

    private string SupportTitle => _isEnglish ? "Support" : "Soporte";
    private string SupportText => _isEnglish
        ? "For questions, issues, or feature requests, visit our GitHub repository or contact us:"
        : "Para preguntas, problemas o solicitudes de funciones, visite nuestro repositorio de GitHub o contáctenos:";

    private string ThankYouText => _isEnglish
        ? "Thank you for using Control Peso Thiscloud! Track your weight. Achieve your goals. Stay healthy."
        : "¡Gracias por usar Control Peso Thiscloud! Sigue tu peso. Alcanza tus objetivos. Mantente saludable.";

    protected override void OnInitialized()
    {
        _isEnglish = false; // Default to Spanish
    }

    private void ToggleLanguage()
    {
        _isEnglish = !_isEnglish;
    }
}
