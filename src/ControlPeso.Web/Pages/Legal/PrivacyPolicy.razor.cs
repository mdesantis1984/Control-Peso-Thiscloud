namespace ControlPeso.Web.Pages.Legal;

public partial class PrivacyPolicy
{
    private bool _isEnglish;

    // Dynamic properties based on language
    private string PageTitleText => _isEnglish ? "Privacy Policy - Control Peso Thiscloud" : "Política de Privacidad - Control Peso Thiscloud";
    private string MetaDescription => _isEnglish
        ? "Learn how Control Peso Thiscloud collects, uses, and protects your personal data. Our Privacy Policy explains your rights and our commitment to data security."
        : "Conozca cómo Control Peso Thiscloud recopila, usa y protege sus datos personales. Nuestra Política de Privacidad explica sus derechos y nuestro compromiso con la seguridad de datos.";

    private string Title => _isEnglish ? "Privacy Policy" : "Política de Privacidad";
    private string LanguageSwitchTooltip => _isEnglish ? "Switch to Spanish" : "Cambiar a Inglés";

    private string EffectiveDateLabel => _isEnglish ? "Effective Date" : "Fecha de Vigencia";
    private string EffectiveDate => "February 25, 2026";
    private string LastUpdatedLabel => _isEnglish ? "Last Updated" : "Última Actualización";
    private string LastUpdated => "February 25, 2026";

    private string IntroductionTitle => _isEnglish ? "Introduction" : "Introducción";
    private string IntroductionText => _isEnglish
        ? "Control Peso Thiscloud respects your privacy and is committed to protecting your personal data. This Privacy Policy explains how we collect, use, disclose, and safeguard your information when you use our weight tracking application."
        : "Control Peso Thiscloud respeta su privacidad y se compromete a proteger sus datos personales. Esta Política de Privacidad explica cómo recopilamos, usamos, divulgamos y protegemos su información cuando utiliza nuestra aplicación de seguimiento de peso.";

    private string AcceptanceText => _isEnglish
        ? "By using the Service, you agree to the collection and use of information in accordance with this policy."
        : "Al utilizar el Servicio, usted acepta la recopilación y el uso de información de acuerdo con esta política.";

    private string InfoCollectedTitle => _isEnglish ? "Information We Collect" : "Información que Recopilamos";

    private string DirectInfoTitle => _isEnglish ? "1. Information You Provide Directly" : "1. Información que Usted Proporciona Directamente";
    private string GoogleAccountLabel => _isEnglish ? "Google Account Information" : "Información de Cuenta de Google";
    private string GoogleAccountInfo => _isEnglish
        ? "Name, email address, profile picture (optional), Google User ID (for authentication)"
        : "Nombre, correo electrónico, foto de perfil (opcional), ID de usuario de Google (para autenticación)";

    private string WeightDataLabel => _isEnglish ? "Weight and Health Data" : "Datos de Peso y Salud";
    private string WeightDataInfo => _isEnglish
        ? "Weight measurements, dates and times, personal notes, height, goal weight, starting weight (optional), date of birth (optional)"
        : "Mediciones de peso, fechas y horas, notas personales, altura, peso objetivo, peso inicial (opcional), fecha de nacimiento (opcional)";

    private string PreferencesLabel => _isEnglish ? "Preferences and Settings" : "Preferencias y Configuraciones";
    private string PreferencesInfo => _isEnglish
        ? "Unit system (Metric/Imperial), language (English/Spanish), theme (Dark/Light), notification settings, timezone"
        : "Sistema de unidades (Métrico/Imperial), idioma (Inglés/Español), tema (Oscuro/Claro), configuración de notificaciones, zona horaria";

    private string AutoCollectedTitle => _isEnglish ? "2. Information Collected Automatically" : "2. Información Recopilada Automáticamente";
    private string UsageDataLabel => _isEnglish ? "Usage Data" : "Datos de Uso";
    private string UsageDataInfo => _isEnglish
        ? "Pages visited, features used, time spent, browser type/version, device type, IP address (anonymized)"
        : "Páginas visitadas, funciones utilizadas, tiempo transcurrido, tipo/versión del navegador, tipo de dispositivo, dirección IP (anonimizada)";

    private string CookiesLabel => _isEnglish ? "Cookies and Similar Technologies" : "Cookies y Tecnologías Similares";
    private string CookiesInfo => _isEnglish
        ? "Authentication cookies (session management), preference cookies (theme, language, settings), analytics cookies (Google Analytics 4)"
        : "Cookies de autenticación (gestión de sesiones), cookies de preferencias (tema, idioma, configuraciones), cookies analíticas (Google Analytics 4)";

    private string HowWeUseTitle => _isEnglish ? "How We Use Your Information" : "Cómo Usamos Su Información";
    private string UseProvide => _isEnglish
        ? "Provide and Maintain the Service: Authenticate identity, store and display weight data, calculate statistics/trends/projections, save preferences"
        : "Proporcionar y Mantener el Servicio: Autenticar identidad, almacenar y mostrar datos de peso, calcular estadísticas/tendencias/proyecciones, guardar preferencias";

    private string UseImprove => _isEnglish
        ? "Improve and Personalize: Analyze usage patterns, customize user experience, develop new features"
        : "Mejorar y Personalizar: Analizar patrones de uso, personalizar experiencia del usuario, desarrollar nuevas funciones";

    private string UseCommunication => _isEnglish
        ? "Communication: Send system notifications (if enabled), respond to inquiries, send important updates"
        : "Comunicación: Enviar notificaciones del sistema (si están habilitadas), responder consultas, enviar actualizaciones importantes";

    private string UseSecurity => _isEnglish
        ? "Security and Compliance: Detect and prevent fraud, monitor security threats, comply with legal obligations"
        : "Seguridad y Cumplimiento: Detectar y prevenir fraude, monitorear amenazas de seguridad, cumplir obligaciones legales";

    private string UseAnalytics => _isEnglish
        ? "Analytics and Research: Understand user interactions, generate anonymized statistics, improve performance"
        : "Análisis e Investigación: Comprender interacciones del usuario, generar estadísticas anonimizadas, mejorar rendimiento";

    private string StorageSecurityTitle => _isEnglish ? "Data Storage and Security" : "Almacenamiento y Seguridad de Datos";
    private string SecurityMeasureHeader => _isEnglish ? "Security Measure" : "Medida de Seguridad";
    private string DetailsHeader => _isEnglish ? "Details" : "Detalles";

    private string EncryptionLabel => _isEnglish ? "Encryption" : "Cifrado";
    private string EncryptionDetails => _isEnglish
        ? "HTTPS/TLS encryption for data in transit"
        : "Cifrado HTTPS/TLS para datos en tránsito";

    private string DatabaseLabel => _isEnglish ? "Database Security" : "Seguridad de Base de Datos";
    private string DatabaseDetails => _isEnglish
        ? "SQL Server with access controls and encryption at rest"
        : "SQL Server con controles de acceso y cifrado en reposo";

    private string AuthenticationLabel => _isEnglish ? "Authentication" : "Autenticación";
    private string AuthenticationDetails => _isEnglish
        ? "Google OAuth 2.0 - We do NOT store passwords"
        : "Google OAuth 2.0 - NO almacenamos contraseñas";

    private string YourRightsTitle => _isEnglish ? "Your Rights and Choices" : "Sus Derechos y Opciones";

    private string AccessRightTitle => _isEnglish ? "Access and Portability" : "Acceso y Portabilidad";
    private string AccessRightText => _isEnglish
        ? "You can access your weight data at any time through the Service. You can request a copy of your data in a portable format by contacting us."
        : "Puede acceder a sus datos de peso en cualquier momento a través del Servicio. Puede solicitar una copia de sus datos en un formato portable contactándonos.";

    private string CorrectionRightTitle => _isEnglish ? "Correction" : "Corrección";
    private string CorrectionRightText => _isEnglish
        ? "You can edit your profile information, weight entries, and preferences directly in the application."
        : "Puede editar su información de perfil, entradas de peso y preferencias directamente en la aplicación.";

    private string DeletionRightTitle => _isEnglish ? "Deletion" : "Eliminación";
    private string DeletionRightText => _isEnglish
        ? "You can delete individual weight entries at any time. You can request full account deletion by contacting us. Upon deletion, we will remove all your personal data within 30 days."
        : "Puede eliminar entradas de peso individuales en cualquier momento. Puede solicitar la eliminación completa de su cuenta contactándonos. Tras la eliminación, eliminaremos todos sus datos personales dentro de los 30 días.";

    private string OptOutRightTitle => _isEnglish ? "Opt-Out of Analytics" : "Optar por No Participar en Análisis";
    private string OptOutRightText => _isEnglish
        ? "You can opt out of Google Analytics tracking by using browser extensions like Google Analytics Opt-out (https://tools.google.com/dlpage/gaoptout)."
        : "Puede optar por no participar en el seguimiento de Google Analytics utilizando extensiones de navegador como Google Analytics Opt-out (https://tools.google.com/dlpage/gaoptout?hl=es).";

    private string ContactTitle => _isEnglish ? "Contact Us" : "Contáctenos";
    private string ContactText => _isEnglish
        ? "If you have questions, concerns, or requests regarding this Privacy Policy or our data practices, please contact us at:"
        : "Si tiene preguntas, inquietudes o solicitudes relacionadas con esta Política de Privacidad o nuestras prácticas de datos, contáctenos en:";

    private string SummaryTitle => _isEnglish ? "Summary of Key Points" : "Resumen de Puntos Clave";
    private string TopicHeader => _isEnglish ? "Topic" : "Tema";
    private string SummaryHeader => _isEnglish ? "Summary" : "Resumen";

    private List<SummaryItem> SummaryItems => _isEnglish
        ? new List<SummaryItem>
        {
            new("Data Collected", "Google account info, weight measurements, preferences, usage data, analytics"),
            new("How We Use It", "Provide service, improve features, security, analytics"),
            new("Data Sharing", "We do NOT sell your data. Shared only with Google (auth/analytics) and hosting provider"),
            new("Your Rights", "Access, correct, delete your data anytime"),
            new("Security", "HTTPS encryption, secure database, Google OAuth (no password storage)"),
            new("Cookies", "Session (essential), preferences, analytics (Google Analytics 4)"),
            new("Children", "Service not intended for children under 13"),
            new("Changes", "We may update this policy; check 'Last Updated' date regularly")
        }
        : new List<SummaryItem>
        {
            new("Datos Recopilados", "Info de cuenta Google, mediciones de peso, preferencias, datos de uso, análisis"),
            new("Cómo los Usamos", "Proporcionar servicio, mejorar funciones, seguridad, análisis"),
            new("Compartir Datos", "NO vendemos sus datos. Compartidos solo con Google (auth/analytics) y proveedor de hosting"),
            new("Sus Derechos", "Acceder, corregir, eliminar sus datos en cualquier momento"),
            new("Seguridad", "Cifrado HTTPS, base de datos segura, Google OAuth (sin almacenamiento de contraseñas)"),
            new("Cookies", "Sesión (esenciales), preferencias, analíticas (Google Analytics 4)"),
            new("Menores", "Servicio no destinado a menores de 13 años"),
            new("Cambios", "Podemos actualizar esta política; verifique fecha de 'Última Actualización' regularmente")
        };

    private string AcknowledgmentText => _isEnglish
        ? "By using Control Peso Thiscloud, you acknowledge that you have read and understood this Privacy Policy."
        : "Al usar Control Peso Thiscloud, usted reconoce que ha leído y comprendido esta Política de Privacidad.";

    protected override void OnInitialized()
    {
        // Default to Spanish (can be changed to detect user's browser language)
        _isEnglish = false;
    }

    private void ToggleLanguage()
    {
        _isEnglish = !_isEnglish;
    }

    private sealed record SummaryItem(string Topic, string Summary);
}
