using Microsoft.AspNetCore.Authentication;

namespace ControlPeso.Web.Extensions;

/// <summary>
/// Extensiones para registrar endpoints de la aplicación.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Registra todos los endpoints de autenticación OAuth.
    /// Incluye Challenge endpoints para Google y LinkedIn.
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
            var properties = new AuthenticationProperties
            {
                RedirectUri = returnUrl ?? "/"
            };
            return Results.Challenge(properties, ["Google"]);
        })
        .WithName("LoginGoogle")
        .RequireRateLimiting("oauth"); // Rate limit: 5 attempts per minute

        // LinkedIn OAuth Challenge endpoint
        authGroup.MapGet("/login/linkedin", (string? returnUrl) =>
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = returnUrl ?? "/"
            };
            return Results.Challenge(properties, ["LinkedIn"]);
        })
        .WithName("LoginLinkedIn")
        .RequireRateLimiting("oauth"); // Rate limit: 5 attempts per minute

        // Logout endpoint (GET para compatibilidad con Blazor NavigationManager)
        authGroup.MapGet("/logout", async (HttpContext context, string? returnUrl) =>
        {
            await context.SignOutAsync();
            return Results.Redirect(returnUrl ?? "/login");
        })
        .WithName("Logout");

        return app;
    }
}
