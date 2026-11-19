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
    private string ChangedLabel => _isEnglish ? "Changed" : "Cambiado";
    private string SecurityLabel => _isEnglish ? "Security" : "Seguridad";
    private string RemovedLabel => _isEnglish ? "Removed" : "Eliminado";
    private string CoreFeaturesLabel => _isEnglish ? "Core Features" : "Funciones Principales";

    // v9.0.0 (2026-11-19)
    private string V900_Date => LegalDateFormatter.Format(new DateOnly(2026, 11, 19), _isEnglish);
    private string V900_Note => _isEnglish
        ? "From this version on the application version follows the release tags (v9.0.0); the earlier app-internal numbering was 1.x."
        : "Desde esta versión el número de versión de la aplicación sigue los tags de release (v9.0.0); la numeración interna anterior de la aplicación era 1.x.";
    private string V900_Changed1 => _isEnglish
        ? "Database: migrated to PostgreSQL 15 (Npgsql); the schema is applied with scripts/migrate-postgres.sh and the application no longer migrates the database on startup"
        : "Base de datos: migrada a PostgreSQL 15 (Npgsql); el esquema se aplica con scripts/migrate-postgres.sh y la aplicación ya no migra la base de datos al iniciar";
    private string V900_Changed2 => _isEnglish
        ? "Imaging: profile image processing uses SkiaSharp (MIT) instead of ImageSharp"
        : "Imágenes: el procesamiento de imágenes de perfil usa SkiaSharp (MIT) en lugar de ImageSharp";
    private string V900_Changed3 => _isEnglish
        ? "Dependencies: updated to MudBlazor 9 and FluentValidation 12; third-party license list refreshed"
        : "Dependencias: actualizadas a MudBlazor 9 y FluentValidation 12; lista de licencias de terceros actualizada";
    private string V900_Added1 => _isEnglish
        ? "Persistent sign-in: the Google sign-in cookie lasts 30 days with sliding expiration"
        : "Sesión persistente: la cookie de inicio de sesión con Google dura 30 días con expiración deslizante";
    private string V900_Added2 => _isEnglish
        ? "Health checks: /health (liveness) and /health/ready (database readiness)"
        : "Health checks: /health (vivacidad) y /health/ready (disponibilidad de la base de datos)";
    private string V900_Added3 => _isEnglish
        ? "Backups: daily pg_dump backups with 14-day retention"
        : "Backups: respaldos diarios con pg_dump y retención de 14 días";
    private string V900_Added4 => _isEnglish
        ? "CI: build, tests, vulnerable-package check, secret scanning (gitleaks) on pull requests and a Docker smoke test"
        : "CI: build, tests, verificación de paquetes vulnerables, escaneo de secretos (gitleaks) en pull requests y smoke test de Docker";
    private string V900_Security1 => _isEnglish
        ? "The container now runs as a non-root user; Data Protection keys are persisted in a dedicated volume"
        : "El contenedor ahora se ejecuta con un usuario sin privilegios de root; las claves de Data Protection se persisten en un volumen dedicado";
    private string V900_Security2 => _isEnglish
        ? "Hardening pass over security headers, configuration and secrets handling"
        : "Revisión de endurecimiento de cabeceras de seguridad, configuración y manejo de secretos";
    private string V900_Removed1 => _isEnglish
        ? "Telegram error notifications: errors are now recorded only in the structured application logs (NDJSON)"
        : "Notificaciones de errores por Telegram: los errores se registran únicamente en los logs estructurados de la aplicación (NDJSON)";
    private string V900_Removed2 => _isEnglish
        ? "Demo pages (Counter, Weather, test pages) and the SQL Server and SQLite runtime providers"
        : "Páginas de demostración (Counter, Weather, páginas de prueba) y los proveedores de runtime SQL Server y SQLite";
    private string V900_Removed3 => _isEnglish
        ? "Google Analytics is only loaded when it is configured"
        : "Google Analytics solo se carga cuando está configurado";

    // v1.3.0-initial (2026-02-25)
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
        _isEnglish = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en"; // Default to Spanish
    }

    private void ToggleLanguage()
    {
        _isEnglish = !_isEnglish;
    }
}
