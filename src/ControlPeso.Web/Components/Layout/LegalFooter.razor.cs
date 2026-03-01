using Microsoft.AspNetCore.Components;

namespace ControlPeso.Web.Components.Layout;

public partial class LegalFooter
{
    [Parameter]
    public bool IsEnglish { get; set; }

    private string PrivacyLabel => IsEnglish ? "Privacy Policy" : "Política de Privacidad";
    private string TermsLabel => IsEnglish ? "Terms & Conditions" : "Términos y Condiciones";
    private string LicensesLabel => IsEnglish ? "Third-Party Licenses" : "Licencias de Terceros";
    private string ChangelogLabel => IsEnglish ? "Changelog" : "Historial de Cambios";

    private int CurrentYear => DateTime.Now.Year;

    private string CopyrightText => IsEnglish
        ? "All rights reserved."
        : "Todos los derechos reservados.";

    private string DisclaimerText => IsEnglish
        ? "For informational purposes only. Not medical advice. Consult qualified healthcare professionals before making health-related decisions."
        : "Solo con fines informativos. No constituye asesoramiento médico. Consulte profesionales de la salud calificados antes de tomar decisiones relacionadas con la salud.";
}
