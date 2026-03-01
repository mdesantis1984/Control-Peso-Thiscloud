namespace ControlPeso.Web.Pages.Legal;

public partial class TermsAndConditions
{
    private bool _isEnglish;

    private string PageTitleText => _isEnglish ? "Terms and Conditions - Control Peso Thiscloud" : "Términos y Condiciones - Control Peso Thiscloud";
    private string MetaDescription => _isEnglish
        ? "Read the Terms and Conditions for Control Peso Thiscloud. Important disclaimers regarding health, liability, and use of the service."
        : "Lea los Términos y Condiciones de Control Peso Thiscloud. Descargos de responsabilidad importantes sobre salud, responsabilidad y uso del servicio.";

    private string Title => _isEnglish ? "Terms and Conditions" : "Términos y Condiciones";
    private string LanguageSwitchTooltip => _isEnglish ? "Switch to Spanish" : "Cambiar a Inglés";

    private string ImportantNoticeTitle => _isEnglish ? "IMPORTANT LEGAL NOTICE" : "AVISO LEGAL IMPORTANTE";
    private string ImportantNoticeText => _isEnglish
        ? "PLEASE READ THESE TERMS AND CONDITIONS CAREFULLY BEFORE USING THIS SERVICE. By accessing or using Control Peso Thiscloud, you acknowledge that you have read, understood, and agree to be bound by these Terms. If you do not agree, you must not use the Service."
        : "POR FAVOR LEA ESTOS TÉRMINOS Y CONDICIONES CUIDADOSAMENTE ANTES DE USAR ESTE SERVICIO. Al acceder o usar Control Peso Thiscloud, usted reconoce que ha leído, comprendido y acepta quedar obligado por estos Términos. Si no está de acuerdo, NO debe usar el Servicio.";

    private string DisclaimerTitle => _isEnglish ? "DISCLAIMER OF LIABILITY - USE AT YOUR OWN RISK" : "DESCARGO DE RESPONSABILIDAD - USO BAJO SU PROPIO RIESGO";
    private string Disclaimer1 => _isEnglish
        ? "This service is for informational and tracking purposes ONLY. It is NOT medical advice."
        : "Este servicio es solo con fines informativos y de seguimiento. NO constituye asesoramiento médico.";
    private string Disclaimer2 => _isEnglish
        ? "Weight management decisions should ALWAYS be made in consultation with qualified healthcare professionals."
        : "Las decisiones sobre manejo del peso SIEMPRE deben tomarse en consulta con profesionales de la salud calificados.";
    private string Disclaimer3 => _isEnglish
        ? "We are NOT liable for health consequences, injuries, or adverse effects from using this service."
        : "NO somos responsables por consecuencias de salud, lesiones o efectos adversos por usar este servicio.";
    private string Disclaimer4 => _isEnglish
        ? "You use this service entirely at your own risk and responsibility."
        : "Usa este servicio completamente bajo su propio riesgo y responsabilidad.";

    private string AsIsTitle => _isEnglish ? "\"AS IS\" AND \"AS AVAILABLE\" BASIS" : "BASE \"TAL CUAL\" Y \"SEGÚN DISPONIBILIDAD\"";
    private string AsIsText => _isEnglish
        ? "THE SERVICE IS PROVIDED ON AN \"AS IS\" AND \"AS AVAILABLE\" BASIS WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED:"
        : "EL SERVICIO SE PROPORCIONA EN UNA BASE \"TAL CUAL\" Y \"SEGÚN DISPONIBILIDAD\" SIN GARANTÍAS DE NINGÚN TIPO, YA SEAN EXPRESAS O IMPLÍCITAS:";

    private string NoWarrantyHeader => _isEnglish ? "No Warranty" : "Sin Garantía";
    private string ExplanationHeader => _isEnglish ? "Explanation" : "Explicación";

    private string NoMerchantability => _isEnglish ? "Merchantability" : "Comerciabilidad";
    private string NoMerchantabilityText => _isEnglish
        ? "We do not warrant that the Service is suitable for any particular purpose."
        : "No garantizamos que el Servicio sea adecuado para ningún propósito particular.";

    private string NoAccuracy => _isEnglish ? "Accuracy" : "Exactitud";
    private string NoAccuracyText => _isEnglish
        ? "We do not warrant that information provided is accurate, complete, or up-to-date."
        : "No garantizamos que la información proporcionada sea precisa, completa o actualizada.";

    private string NoReliability => _isEnglish ? "Reliability" : "Confiabilidad";
    private string NoReliabilityText => _isEnglish
        ? "We do not warrant that the Service will be uninterrupted, error-free, or free from viruses."
        : "No garantizamos que el Servicio esté ininterrumpido, libre de errores o libre de virus.";

    private string NoDataLoss => _isEnglish ? "Data Loss" : "Pérdida de Datos";
    private string NoDataLossText => _isEnglish
        ? "We do not guarantee that your data will be stored securely or without loss."
        : "No garantizamos que sus datos se almacenen de forma segura o sin pérdida.";

    private string LiabilityTitle => _isEnglish ? "LIMITATION OF LIABILITY" : "LIMITACIÓN DE RESPONSABILIDAD";
    private string LiabilityMainText => _isEnglish
        ? "TO THE MAXIMUM EXTENT PERMITTED BY LAW, WE SHALL NOT BE LIABLE FOR:"
        : "EN LA MÁXIMA MEDIDA PERMITIDA POR LA LEY, NO SEREMOS RESPONSABLES POR:";

    private string LiabilityHealth => _isEnglish
        ? "Health complications, injuries, adverse effects from weight management activities"
        : "Complicaciones de salud, lesiones, efectos adversos de actividades de manejo del peso";
    private string LiabilityData => _isEnglish
        ? "Loss of data, including weight records, notes, or personal information"
        : "Pérdida de datos, incluidos registros de peso, notas o información personal";
    private string LiabilityService => _isEnglish
        ? "Service interruptions, downtime, technical errors, bugs, or malfunctions"
        : "Interrupciones del servicio, tiempo de inactividad, errores técnicos, bugs o mal funcionamiento";
    private string LiabilityThirdParty => _isEnglish
        ? "Third-party actions (Google, hosting providers, etc.)"
        : "Acciones de terceros (Google, proveedores de hosting, etc.)";

    private string MaxLiabilityText => _isEnglish
        ? "MAXIMUM LIABILITY (since this service is provided free of charge):"
        : "RESPONSABILIDAD MÁXIMA (dado que este servicio se proporciona de forma gratuita):";
    private string ZeroLiability => "USD $0.00";

    private string UserResponsibilitiesTitle => _isEnglish ? "User Responsibilities" : "Responsabilidades del Usuario";

    private string AccurateInfoTitle => _isEnglish ? "Accurate Information" : "Información Precisa";
    private string AccurateInfoText => _isEnglish
        ? "You are responsible for providing accurate and truthful information. You must update your profile information if it changes."
        : "Usted es responsable de proporcionar información precisa y veraz. Debe actualizar la información de su perfil si cambia.";

    private string AccountSecurityTitle => _isEnglish ? "Account Security" : "Seguridad de la Cuenta";
    private string AccountSecurityText => _isEnglish
        ? "You are responsible for maintaining the confidentiality of your Google account credentials. You are responsible for all activities under your account. Notify us immediately if you suspect unauthorized access."
        : "Usted es responsable de mantener la confidencialidad de las credenciales de su cuenta de Google. Usted es responsable de todas las actividades bajo su cuenta. Notifíquenos inmediatamente si sospecha de acceso no autorizado.";

    private string ProhibitedUsesTitle => _isEnglish ? "Prohibited Uses" : "Usos Prohibidos";
    private string ProhibitedUsesIntro => _isEnglish
        ? "YOU AGREE NOT TO:"
        : "USTED ACEPTA NO:";
    private string Prohibited1 => _isEnglish
        ? "Use the Service for unlawful purposes or in violation of these Terms"
        : "Usar el Servicio para propósitos ilegales o en violación de estos Términos";
    private string Prohibited2 => _isEnglish
        ? "Attempt to gain unauthorized access to servers or databases"
        : "Intentar obtener acceso no autorizado a servidores o bases de datos";
    private string Prohibited3 => _isEnglish
        ? "Interfere with or disrupt the Service"
        : "Interferir con o interrumpir el Servicio";
    private string Prohibited4 => _isEnglish
        ? "Upload malicious code, viruses, or harmful software"
        : "Subir código malicioso, virus o software dañino";

    private string MinorsTitle => _isEnglish ? "Minors" : "Menores";
    private string MinorsText => _isEnglish
        ? "The Service is not intended for children under 13 years of age. If you are between 13 and 18, you must have parental or guardian consent."
        : "El Servicio no está destinado a menores de 13 años. Si tiene entre 13 y 18 años, debe tener el consentimiento de sus padres o tutor.";

    private string TerminationTitle => _isEnglish ? "Termination" : "Terminación";
    private string TerminationText => _isEnglish
        ? "You may stop using the Service at any time and request account deletion by contacting us."
        : "Puede dejar de usar el Servicio en cualquier momento y solicitar la eliminación de cuenta contactándonos.";
    private string TerminationRightsText => _isEnglish
        ? "WE RESERVE THE RIGHT TO SUSPEND OR TERMINATE YOUR ACCESS AT ANY TIME, WITH OR WITHOUT NOTICE, for violations of these Terms, fraudulent activity, prolonged inactivity, or technical/security reasons."
        : "NOS RESERVAMOS EL DERECHO DE SUSPENDER O TERMINAR SU ACCESO EN CUALQUIER MOMENTO, CON O SIN AVISO, por violaciones de estos Términos, actividad fraudulenta, inactividad prolongada o razones técnicas/de seguridad.";

    private string ChangesTitle => _isEnglish ? "Changes to the Service and Terms" : "Cambios al Servicio y a los Términos";
    private string ChangesText => _isEnglish
        ? "WE RESERVE THE RIGHT TO MODIFY, SUSPEND, OR DISCONTINUE THE SERVICE (or any part thereof) AT ANY TIME, WITH OR WITHOUT NOTICE. We may update these Terms from time to time. Changes are effective immediately upon posting. Your continued use constitutes acceptance of updated Terms."
        : "NOS RESERVAMOS EL DERECHO DE MODIFICAR, SUSPENDER O DESCONTINUAR EL SERVICIO (o cualquier parte del mismo) EN CUALQUIER MOMENTO, CON O SIN AVISO. Podemos actualizar estos Términos ocasionalmente. Los cambios son efectivos inmediatamente al publicarse. Su uso continuado constituye la aceptación de los Términos actualizados.";

    private string GoverningLawTitle => _isEnglish ? "Governing Law" : "Ley Aplicable";
    private string GoverningLawText => _isEnglish
        ? "These Terms shall be governed by and construed in accordance with the laws of Argentina."
        : "Estos Términos se regirán e interpretarán de acuerdo con las leyes de Argentina.";
    private string Argentina => "Argentina";

    private string ContactTitle => _isEnglish ? "Contact Us" : "Contáctenos";

    private string FinalAcknowledgmentTitle => _isEnglish ? "FINAL ACKNOWLEDGMENT" : "RECONOCIMIENTO FINAL";
    private string Acknowledgment1 => _isEnglish
        ? "You have read and understood these Terms in their entirety"
        : "Ha leído y comprendido estos Términos en su totalidad";
    private string Acknowledgment2 => _isEnglish
        ? "You accept full responsibility for your use of the Service"
        : "Acepta la responsabilidad total por su uso del Servicio";
    private string Acknowledgment3 => _isEnglish
        ? "You understand that the Service is NOT a substitute for medical advice"
        : "Comprende que el Servicio NO es un sustituto del asesoramiento médico";
    private string Acknowledgment4 => _isEnglish
        ? "You agree to consult with healthcare professionals before making health decisions"
        : "Acepta consultar con profesionales de la salud antes de tomar decisiones de salud";
    private string Acknowledgment5 => _isEnglish
        ? "You use the Service entirely at your own risk"
        : "Usa el Servicio completamente bajo su propio riesgo";
    private string Acknowledgment6 => _isEnglish
        ? "You agree to indemnify and hold harmless Control Peso Thiscloud from any claims"
        : "Acepta indemnizar y eximir de responsabilidad a Control Peso Thiscloud de cualquier reclamo";

    private string LastUpdatedLabel => _isEnglish ? "Last Updated" : "Última Actualización";
    private string LastUpdated => "February 25, 2026";
    private string EffectiveDateLabel => _isEnglish ? "Effective Date" : "Fecha de Vigencia";
    private string EffectiveDate => "February 25, 2026";
    private string Subtitle => _isEnglish ? "Weight Tracking Tool - For informational purposes only. Not medical advice." : "Herramienta de Seguimiento de Peso - Solo con fines informativos. No constituye asesoramiento médico.";

    protected override void OnInitialized()
    {
        // Default to Spanish
        _isEnglish = false;
    }

    private void ToggleLanguage()
    {
        _isEnglish = !_isEnglish;
    }
}
