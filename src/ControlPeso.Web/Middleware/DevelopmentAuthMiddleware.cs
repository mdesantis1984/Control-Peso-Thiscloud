using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ControlPeso.Web.Middleware;

/// <summary>
/// Middleware de autenticación fake para Development - permite bypass de Google OAuth durante debugging con MCP.
/// Google detecta Chrome automatizado (MCP) como bot y bloquea el login.
/// Este middleware simula un usuario autenticado para permitir testing de páginas [Authorize].
/// SOLO SE ACTIVA EN DEVELOPMENT - NUNCA EN PRODUCCIÓN.
/// </summary>
internal sealed class DevelopmentAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DevelopmentAuthMiddleware> _logger;

    // IMPORTANTE: Valores del usuario REAL en la base de datos
    // Obtenidos de: SELECT Id, Email, Name, GoogleId FROM Users LIMIT 1;
    private const string FakeUserId = "550e8400-e29b-41d4-a716-446655440001"; // GUID REAL de BD
    private const string FakeEmail = "marco.desantis@thiscloud.com"; // EMAIL REAL de BD
    private const string FakeName = "Marco De Santis"; // NOMBRE REAL de BD
    private const string FakeGoogleId = "google_demo_admin_001"; // GOOGLE ID REAL de BD

    public DevelopmentAuthMiddleware(RequestDelegate next, ILogger<DevelopmentAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IHostEnvironment environment)
    {
        // SECURITY: Fail-fast if accidentally registered in non-Development
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "DevelopmentAuthMiddleware MUST NOT be used outside Development environment. " +
                "This middleware bypasses authentication for debugging and poses a critical security risk in Production.");
        }

        // Solo aplicar si NO está ya autenticado
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogDebug("Development Auth Middleware: Injecting fake authenticated user - UserId: {UserId}, Email: {Email}",
                FakeUserId, FakeEmail);

            // Crear claims del usuario fake (simulando UserClaimsTransformation)
            var claims = new List<Claim>
            {
                // Claims de Google OAuth (NameIdentifier = Google ID al inicio)
                new(ClaimTypes.NameIdentifier, FakeUserId), // IMPORTANTE: Después de UserClaimsTransformation es el UserId GUID
                new(ClaimTypes.Email, FakeEmail),
                new(ClaimTypes.Name, FakeName),
                new("google_id", FakeGoogleId),
                
                // Claims custom de nuestra app (agregados por UserClaimsTransformation)
                new("user_id", FakeUserId), // GUID del usuario en BD
                new("role", "User"), // UserRole.User
                new("status", "Active") // UserStatus.Active
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Asignar principal al contexto
            context.User = principal;

            // OPCIONAL: Firmar cookie de autenticación para persistencia entre requests
            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            _logger.LogInformation("Development Auth Middleware: User authenticated - UserId: {UserId}, Email: {Email}, Name: {Name}",
                FakeUserId, FakeEmail, FakeName);
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods para registrar el middleware de autenticación fake en Development.
/// </summary>
internal static class DevelopmentAuthMiddlewareExtensions
{
    /// <summary>
    /// Registra el middleware de autenticación fake SOLO en Development.
    /// Permite testing de páginas [Authorize] sin hacer login real con Google OAuth.
    /// </summary>
    /// <param name="app">Application builder.</param>
    /// <param name="environment">Environment para verificar si es Development.</param>
    /// <returns>Application builder para chaining.</returns>
    public static IApplicationBuilder UseDevelopmentAuth(this IApplicationBuilder app, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            var logger = app.ApplicationServices.GetRequiredService<ILogger<DevelopmentAuthMiddleware>>();
            logger.LogWarning("=== DEVELOPMENT AUTH MIDDLEWARE ENABLED - Bypassing Google OAuth for MCP debugging ===");
            logger.LogWarning("NEVER enable this in Production - security risk!");

            app.UseMiddleware<DevelopmentAuthMiddleware>();
        }

        return app;
    }
}
