using ControlPeso.Application.DTOs;
using ControlPeso.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Security.Claims;

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
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = "Google"; // Default challenge (puede ser Google o LinkedIn)
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, ConfigureCookieOptions)
        .AddGoogle("Google", options => ConfigureGoogleOptions(options, configuration))
        .AddLinkedIn(options => ConfigureLinkedInOptions(options, configuration));

        // 2. Agregar servicios de autorización
        services.AddAuthorization();

        // 3. Agregar cascading authentication state para Blazor
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
        var googleConfig = configuration.GetSection("Authentication:Google");

        options.ClientId = googleConfig["ClientId"]
            ?? throw new InvalidOperationException("Google ClientId not configured in appsettings.json");

        options.ClientSecret = googleConfig["ClientSecret"]
            ?? throw new InvalidOperationException("Google ClientSecret not configured in appsettings.json");

        options.SaveTokens = true;

        // Scopes para obtener información del usuario
        options.Scope.Add("profile");
        options.Scope.Add("email");

        // Event handler: callback después de autenticación exitosa
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                // Obtener servicio de usuario desde DI
                var userService = context.HttpContext.RequestServices
                    .GetRequiredService<IUserService>();

                // Extraer claims del proveedor OAuth (Google)
                // Google mapea 'sub' a ClaimTypes.NameIdentifier automáticamente
                var externalId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.Principal?.FindFirst("sub")?.Value
                    ?? throw new InvalidOperationException("Google 'sub' claim is missing");

                var name = context.Principal?.FindFirst(ClaimTypes.Name)?.Value
                    ?? context.Principal?.FindFirst("name")?.Value
                    ?? string.Empty;

                var email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value
                    ?? context.Principal?.FindFirst("email")?.Value
                    ?? throw new InvalidOperationException("Google 'email' claim is missing");

                var avatarUrl = context.Principal?.FindFirst("picture")?.Value;

                // Crear DTO genérico de OAuth
                var oauthInfo = new OAuthUserInfo
                {
                    Provider = "Google",
                    ExternalId = externalId,
                    Name = name,
                    Email = email,
                    AvatarUrl = avatarUrl
                };

                // Crear o actualizar usuario en DB
                var user = await userService.CreateOrUpdateFromOAuthAsync(oauthInfo);

                // Agregar claims personalizados a la identidad del usuario
                var identity = context.Principal?.Identity as ClaimsIdentity;
                if (identity != null)
                {
                    // Claim del User ID (GUID) - principal para identificar al usuario en la app
                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));

                    // Claim del Role (User o Administrator)
                    identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));

                    // Claim del Email (si no existe ya)
                    if (!identity.HasClaim(c => c.Type == ClaimTypes.Email))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
                    }

                    // Claim del Name (si no existe ya)
                    if (!identity.HasClaim(c => c.Type == ClaimTypes.Name))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Name, user.Name));
                    }
                }
            }
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

        // Event handler: callback después de autenticación exitosa
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                // Obtener servicio de usuario desde DI
                var userService = context.HttpContext.RequestServices
                    .GetRequiredService<IUserService>();

                // Extraer claims del proveedor OAuth (LinkedIn)
                var externalId = context.Principal?.FindFirst("sub")?.Value
                    ?? throw new InvalidOperationException("LinkedIn 'sub' claim is missing");

                var name = context.Principal?.FindFirst("name")?.Value
                    ?? string.Empty;

                var email = context.Principal?.FindFirst("email")?.Value
                    ?? throw new InvalidOperationException("LinkedIn 'email' claim is missing");

                var avatarUrl = context.Principal?.FindFirst("picture")?.Value;

                // Crear DTO genérico de OAuth
                var oauthInfo = new OAuthUserInfo
                {
                    Provider = "LinkedIn",
                    ExternalId = externalId,
                    Name = name,
                    Email = email,
                    AvatarUrl = avatarUrl
                };

                // Crear o actualizar usuario en DB
                var user = await userService.CreateOrUpdateFromOAuthAsync(oauthInfo);

                // Agregar claims personalizados a la identidad del usuario
                var identity = context.Principal?.Identity as ClaimsIdentity;
                if (identity != null)
                {
                    // Claim del User ID (GUID) - principal para identificar al usuario en la app
                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));

                    // Claim del Role (User o Administrator)
                    identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));

                    // Claim del Email (si no existe ya)
                    if (!identity.HasClaim(c => c.Type == ClaimTypes.Email))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
                    }

                    // Claim del Name (si no existe ya)
                    if (!identity.HasClaim(c => c.Type == ClaimTypes.Name))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Name, user.Name));
                    }
                }
            }
        };
    }
}
