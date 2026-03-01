namespace ControlPeso.Web.Pages.Legal;

public partial class ThirdPartyLicenses
{
    private bool _isEnglish;

    private string PageTitleText => _isEnglish ? "Third-Party Licenses - Control Peso Thiscloud" : "Licencias de Terceros - Control Peso Thiscloud";
    private string MetaDescription => _isEnglish
        ? "Control Peso Thiscloud acknowledges and thanks all open-source projects and third-party services used in this application."
        : "Control Peso Thiscloud reconoce y agradece a todos los proyectos de código abierto y servicios de terceros utilizados en esta aplicación.";

    private string Title => _isEnglish ? "Third-Party Licenses & Acknowledgments" : "Licencias de Terceros y Agradecimientos";
    private string LanguageSwitchTooltip => _isEnglish ? "Switch to Spanish" : "Cambiar a Inglés";

    private string IntroText => _isEnglish
        ? "Control Peso Thiscloud is built with the support of many outstanding open-source projects and services. We are deeply grateful to the developers and communities behind these technologies."
        : "Control Peso Thiscloud está construido con el apoyo de muchos proyectos de código abierto y servicios excepcionales. Estamos profundamente agradecidos a los desarrolladores y comunidades detrás de estas tecnologías.";

    private string ThanksText => _isEnglish
        ? "Thank you to everyone who contributes to open source. You make the world a better place. 💙"
        : "Gracias a todos los que contribuyen al código abierto. Hacen del mundo un lugar mejor. 💙";

    private string CoreTitle => _isEnglish ? "Core Framework and Runtime" : "Framework y Runtime Principal";
    private string UiFrameworkTitle => _isEnglish ? "UI Framework" : "Framework de UI";
    private string DatabaseTitle => _isEnglish ? "Database and ORM" : "Base de Datos y ORM";
    private string AuthTitle => _isEnglish ? "Authentication" : "Autenticación";
    private string LoggingTitle => _isEnglish ? "Logging and Observability" : "Logging y Observabilidad";
    private string ValidationTestingTitle => _isEnglish ? "Validation and Testing" : "Validación y Testing";
    private string LicenseSummaryTitle => _isEnglish ? "License Summaries" : "Resúmenes de Licencias";
    private string ComplianceTitle => _isEnglish ? "Compliance Statement" : "Declaración de Cumplimiento";

    private string NameHeader => _isEnglish ? "Name" : "Nombre";
    private string VersionHeader => _isEnglish ? "Version" : "Versión";
    private string LicenseHeader => _isEnglish ? "License" : "Licencia";
    private string LinkHeader => _isEnglish ? "Link" : "Enlace";
    private string WebsiteLabel => _isEnglish ? "Website" : "Sitio Web";

    private string MudBlazorDescription => _isEnglish
        ? "MudBlazor is the backbone of our user interface. Thank you to the MudBlazor team and community for creating a beautiful, feature-rich component library that brings Material Design to Blazor."
        : "MudBlazor es la columna vertebral de nuestra interfaz de usuario. Gracias al equipo y comunidad de MudBlazor por crear una biblioteca de componentes hermosa y rica en funciones que lleva Material Design a Blazor.";

    private string AuthDescription => _isEnglish
        ? "Thank you to Google for providing a secure, widely-adopted authentication system that protects user accounts without requiring password storage, and to Microsoft for the ASP.NET Core Google authentication provider."
        : "Gracias a Google por proporcionar un sistema de autenticación seguro y ampliamente adoptado que protege las cuentas de usuario sin requerir almacenamiento de contraseñas, y a Microsoft por el proveedor de autenticación Google de ASP.NET Core.";

    private string ThisCloudLoggingDesc => _isEnglish
        ? "Enterprise logging with Serilog, redaction, and correlation"
        : "Logging empresarial con Serilog, redacción y correlación";

    private string SerilogDesc => _isEnglish
        ? "Structured logging library for powerful log analysis"
        : "Biblioteca de logging estructurado para análisis potente de logs";

    private string MitSummary => _isEnglish
        ? "Allows almost unrestricted freedom to use, copy, modify, merge, publish, distribute, sublicense, and sell copies. Requires preservation of copyright notice."
        : "Permite libertad casi sin restricciones para usar, copiar, modificar, fusionar, publicar, distribuir, sublicenciar y vender copias. Requiere preservación del aviso de copyright.";
    private string MitUsed => _isEnglish ? "Used by: .NET, EF Core, MudBlazor, xUnit, bUnit" : "Usada por: .NET, EF Core, MudBlazor, xUnit, bUnit";

    private string ApacheSummary => _isEnglish
        ? "Similar to MIT but includes explicit patent grants. Requires preservation of copyright notice and attribution."
        : "Similar a MIT pero incluye concesiones explícitas de patentes. Requiere preservación del aviso de copyright y atribución.";
    private string ApacheUsed => _isEnglish ? "Used by: FluentValidation, Serilog, Docker" : "Usada por: FluentValidation, Serilog, Docker";

    private string BsdSummary => _isEnglish
        ? "Permissive license similar to MIT, with additional clause prohibiting use of project name for endorsement without permission."
        : "Licencia permisiva similar a MIT, con cláusula adicional que prohíbe el uso del nombre del proyecto para respaldo sin permiso.";
    private string BsdUsed => _isEnglish ? "Used by: Moq" : "Usada por: Moq";

    private string ComplianceText => _isEnglish
        ? "Control Peso Thiscloud complies with all license terms of third-party software and services. We include all required copyright notices, provide attribution to original authors, retain original license texts, and comply with redistribution requirements."
        : "Control Peso Thiscloud cumple con todos los términos de licencia del software y servicios de terceros. Incluimos todos los avisos de copyright requeridos, proporcionamos atribución a los autores originales, conservamos los textos de licencia originales y cumplimos con los requisitos de redistribución.";

    private string LastUpdatedLabel => _isEnglish ? "Last Updated" : "Última Actualización";
    private string ThankYouText => _isEnglish ? "Thank you to everyone who contributes to open source!" : "¡Gracias a todos los que contribuyen al código abierto!";

    private List<Dependency> CoreDependencies => new()
    {
        new(".NET 10", "10.0", "MIT", "https://github.com/dotnet/runtime"),
    };

    private List<Dependency> DatabaseDependencies => new()
    {
        new("Entity Framework Core", "10.0.3", "MIT", "https://github.com/dotnet/efcore"),
        new("EF Core SQLite Provider", "10.0.3", "MIT", "https://github.com/dotnet/efcore"),
        new("EF Core SQL Server Provider", "10.0.3", "MIT", "https://github.com/dotnet/efcore"),
    };

    private List<Dependency> ValidationTestingDependencies => new()
    {
        new("FluentValidation", "11.11.0", "Apache 2.0", "https://github.com/FluentValidation/FluentValidation"),
        new("xUnit.net", "2.9.2", "Apache 2.0 / MIT", "https://github.com/xunit/xunit"),
        new("Moq", "4.20.72", "BSD 3-Clause", "https://github.com/devlooped/moq"),
        new("bUnit", "1.34.7", "MIT", "https://github.com/bUnit-dev/bUnit"),
    };

    protected override void OnInitialized()
    {
        _isEnglish = false; // Default to Spanish
    }

    private void ToggleLanguage()
    {
        _isEnglish = !_isEnglish;
    }

    private sealed record Dependency(string Name, string Version, string License, string Url);
}
