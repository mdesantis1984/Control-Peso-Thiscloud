using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace ControlPeso.Web.Extensions;

/// <summary>
/// Extensiones para configurar autenticación OAuth 2.0 (Google).
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Configura autenticación con Cookie + Google OAuth.
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

        // 3. Agregar servicios de autorización
        services.AddAuthorization();

        // 4. Agregar cascading authentication state para Blazor
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
}
