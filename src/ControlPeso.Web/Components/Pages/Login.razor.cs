using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;

namespace ControlPeso.Web.Components.Pages;

/// <summary>
/// Página de autenticación OAuth con Google.
/// Diseño pixel perfect basado en prototipo Google AI Studio.
/// </summary>
public partial class Login
{
    // ========================================================================
    // DEPENDENCY INJECTION
    // ========================================================================

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private IStringLocalizer<Login> Localizer { get; set; } = null!;

    // ========================================================================
    // LOCALIZED STRINGS (Properties for clean markup)
    // ========================================================================

    // --- Page / Meta ---
    private string PageTitle => Localizer["PageTitle"];
    private string MetaDescription => Localizer["MetaDescription"];
    private string MetaKeywords => Localizer["MetaKeywords"];
    private string OgTitle => Localizer["OgTitle"];
    private string OgDescription => Localizer["OgDescription"];
    private string TwitterTitle => Localizer["TwitterTitle"];
    private string TwitterDescription => Localizer["TwitterDescription"];

    // --- Main Content ---
    private string MainTitle => Localizer["MainTitle"];
    private string MainSubtitle => Localizer["MainSubtitle"];
    private string GoogleSignInButton => Localizer["GoogleSignInButton"];
    private string SecurityBadge => Localizer["SecurityBadge"];
    private string CreateAccountButton => Localizer["CreateAccountButton"];
    private string OAuthFailedMessage => Localizer["OAuthFailedMessage"];

    // --- Footer Links ---
    private string TermsLink => Localizer["TermsLink"];
    private string PrivacyLink => Localizer["PrivacyLink"];
    private string NeedHelp => Localizer["NeedHelp"];
    private string ContactSupport => Localizer["ContactSupport"];

    // ========================================================================
    // CONSTANTS
    // ========================================================================

    /// <summary>
    /// URL de la ilustración de fondo para la página de login.
    /// </summary>
    private const string IllustrationUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuBIq3Wn-6jEEHYdKS6nK3JUFao8Q0NoOzNcsm4sCJkYtu5MAUS1CIbRooBfMwOWomjdsYPqB2kkFh7yb7FnUf40fxzew62lo27gAMeBDxtaX1R5HyiB_sfRddjnqjqY6aEH6OEGZ_ZHCcKPCG-bgRtAV1aPxNDV87fk3q-dyIUR7qY190IUXU4g6z4i1mQmTeWdjvwLjLXTXs3h7XOrweGUhQ1Gv6u5Eddeu15duylQRqmYDGiSVj78nDU0i-Ks9h7H_NHTJY2m7w";

    // ========================================================================
    // STATE
    // ========================================================================

    /// <summary>
    /// True when the OAuth callback redirected here with <c>?error=...</c> (for example <c>oauth_failed</c>).
    /// </summary>
    private bool _showOAuthError;

    protected override void OnInitialized()
    {
        var query = QueryHelpers.ParseQuery(new Uri(NavigationManager.Uri).Query);
        _showOAuthError = query.TryGetValue("error", out var error) && !string.IsNullOrWhiteSpace(error.ToString());
    }

    // ========================================================================
    // METHODS
    // ========================================================================

    /// <summary>
    /// Genera el estilo CSS inline para la ilustración de background.
    /// </summary>
    private string GetIllustrationStyle()
    {
        return $"background-image: url('{IllustrationUrl}'); background-size: cover; background-position: center;";
    }

    /// <summary>
    /// Inicia el flujo de autenticación con Google OAuth.
    /// Redirige al endpoint Challenge de ASP.NET Core Authentication.
    /// </summary>
    private void LoginWithGoogle()
    {
        // Endpoint OAuth Challenge para Google
        // El middleware de autenticación manejará el redirect a Google OAuth
        var returnUrl = NavigationManager.ToAbsoluteUri("/").ToString();
        NavigationManager.NavigateTo(
            $"/api/auth/login/google?returnUrl={Uri.EscapeDataString(returnUrl)}",
            forceLoad: true);
    }
}
