using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace ControlPeso.Web.Extensions;

/// <summary>
/// Extensiones para configurar autenticación OAuth 2.0 (Google + LinkedIn).
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Configura autenticación con Cookie + Google OAuth + LinkedIn OAuth.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration con sección Authentication.</param>
    /// <returns>Service collection para chaining.</returns>
    public static IServiceCollection AddOAuthAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // 1. Configurar esquema de autenticación con Cookies como principal
        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = "Google"; // Default challenge es Google
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, ConfigureCookieOptions);

        // 2. Agregar Google OAuth (si está configurado)
        var googleClientId = configuration.GetValue<string>("Authentication:Google:ClientId");
        if (!string.IsNullOrWhiteSpace(googleClientId))
        {
            authBuilder.AddGoogle("Google", options => ConfigureGoogleOptions(options, configuration));
        }

        // 3. Agregar LinkedIn OAuth (solo si está configurado - OPCIONAL)
        var linkedInClientId = configuration.GetValue<string>("Authentication:LinkedIn:ClientId");
        if (!string.IsNullOrWhiteSpace(linkedInClientId))
        {
            authBuilder.AddLinkedIn(options => ConfigureLinkedInOptions(options, configuration));
        }

        // 4. Agregar servicios de autorización
        services.AddAuthorization();

        // 5. Agregar cascading authentication state para Blazor
        services.AddCascadingAuthenticationState();

        return services;
    }

    /// <summary>
    /// Configura opciones de cookie segura.
    /// </summary>
    private static void ConfigureCookieOptions(CookieAuthenticationOptions options)
    {
        // Rutas de autenticación
        options.LoginPath = "/login";
        options.LogoutPath = "/api/auth/logout";
        options.AccessDeniedPath = "/access-denied";

        // Configuración de seguridad de cookie
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        // Expiración y renovación
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    }

    /// <summary>
    /// Configura opciones de Google OAuth.
    /// </summary>
    private static void ConfigureGoogleOptions(
        Microsoft.AspNetCore.Authentication.Google.GoogleOptions options,
        IConfiguration configuration)
    {
        // Configuración ya validada en AddOAuthAuthentication (ClientId no nulo)
        options.ClientId = configuration.GetValue<string>("Authentication:Google:ClientId")!;
        options.ClientSecret = configuration.GetValue<string>("Authentication:Google:ClientSecret")!;

        options.SaveTokens = true;

        // Scopes para obtener información del usuario
        options.Scope.Add("profile");
        options.Scope.Add("email");

        // Callback after the provider authenticated the user; any failure ends in OnRemoteFailure
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = context => OAuthLoginHandlers.OnCreatingTicketAsync(context, "Google"),
            OnRemoteFailure = OAuthLoginHandlers.OnRemoteFailureAsync
        };
    }

    /// <summary>
    /// Configura opciones de LinkedIn OAuth.
    /// </summary>
    private static void ConfigureLinkedInOptions(
        AspNet.Security.OAuth.LinkedIn.LinkedInAuthenticationOptions options,
        IConfiguration configuration)
    {
        var linkedInConfig = configuration.GetSection("Authentication:LinkedIn");

        options.ClientId = linkedInConfig["ClientId"]
            ?? throw new InvalidOperationException("LinkedIn ClientId not configured in appsettings.json");

        options.ClientSecret = linkedInConfig["ClientSecret"]
            ?? throw new InvalidOperationException("LinkedIn ClientSecret not configured in appsettings.json");

        options.SaveTokens = true;

        // Scopes para obtener información del usuario de LinkedIn
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        // Callback after the provider authenticated the user; any failure ends in OnRemoteFailure
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = context => OAuthLoginHandlers.OnCreatingTicketAsync(context, "LinkedIn"),
            OnRemoteFailure = OAuthLoginHandlers.OnRemoteFailureAsync
        };
    }
}
