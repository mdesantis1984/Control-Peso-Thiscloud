using Microsoft.AspNetCore.Authentication;

namespace ControlPeso.Web.Extensions;

/// <summary>
/// Extensiones para registrar endpoints de la aplicación.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Registra todos los endpoints de autenticación OAuth.
    /// Incluye el Challenge endpoint de Google.
    /// </summary>
    /// <param name="app">WebApplication instance.</param>
    /// <returns>WebApplication para chaining.</returns>
    public static WebApplication MapAuthenticationEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Grupo de endpoints de autenticación
        var authGroup = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .AllowAnonymous();

        // Google OAuth Challenge endpoint
        authGroup.MapGet("/login/google", (string? returnUrl) =>
        {
            return Results.Challenge(CreateChallengeProperties(returnUrl), ["Google"]);
        })
        .WithName("LoginGoogle")
        .RequireRateLimiting("oauth"); // Rate limit: 5 attempts per minute

        // Logout endpoint (GET para compatibilidad con Blazor NavigationManager)
        authGroup.MapGet("/logout", async (HttpContext context, string? returnUrl) =>
        {
            await context.SignOutAsync();
            return Results.Redirect(ToLocalUrl(returnUrl, "/login"));
        })
        .WithName("Logout");

        return app;
    }

    /// <summary>
    /// Builds the OAuth challenge properties: the sign-in cookie is persistent (it survives
    /// browser restarts for the cookie lifetime) and the post-login redirect stays on this site.
    /// </summary>
    internal static AuthenticationProperties CreateChallengeProperties(string? returnUrl) => new()
    {
        RedirectUri = ToLocalUrl(returnUrl, "/"),
        IsPersistent = true
    };

    /// <summary>
    /// Returns <paramref name="url"/> when it is a local path, otherwise <paramref name="fallback"/>.
    /// Rejects absolute URLs and protocol-relative forms such as "//host" and "/\host".
    /// </summary>
    internal static string ToLocalUrl(string? url, string fallback)
    {
        if (string.IsNullOrEmpty(url) || url[0] != '/')
        {
            return fallback;
        }

        if (url.Length > 1 && (url[1] == '/' || url[1] == '\\'))
        {
            return fallback;
        }

        return url;
    }
}
