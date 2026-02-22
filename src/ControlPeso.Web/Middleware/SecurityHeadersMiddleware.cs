namespace ControlPeso.Web.Middleware;

/// <summary>
/// Middleware que agrega headers de seguridad HTTP a todas las respuestas.
/// Implementa protecciones contra XSS, clickjacking, content sniffing y otras vulnerabilidades web.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // NO aplicar CSP a archivos estáticos (imágenes, CSS, JS, fuentes)
        // Esto permite que las imágenes de /uploads/avatars/ se carguen sin restricciones
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var isStaticFile = path.StartsWith("/uploads/") || 
                          path.StartsWith("/_content/") || 
                          path.StartsWith("/css/") || 
                          path.StartsWith("/js/") || 
                          path.StartsWith("/images/") ||
                          path.StartsWith("/fonts/") ||
                          path.EndsWith(".css") || 
                          path.EndsWith(".js") || 
                          path.EndsWith(".png") || 
                          path.EndsWith(".jpg") || 
                          path.EndsWith(".jpeg") || 
                          path.EndsWith(".gif") || 
                          path.EndsWith(".svg") || 
                          path.EndsWith(".ico") ||
                          path.EndsWith(".woff") ||
                          path.EndsWith(".woff2") ||
                          path.EndsWith(".ttf") ||
                          path.EndsWith(".eot");

        if (!isStaticFile)
        {
            // Solo aplicar headers de seguridad a respuestas HTML/API

            // X-Content-Type-Options: Previene MIME type sniffing
            // El navegador debe respetar el Content-Type declarado
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            // X-Frame-Options: Previene clickjacking
            // DENY: La página NO puede ser embebida en iframes
            context.Response.Headers["X-Frame-Options"] = "DENY";

            // X-XSS-Protection: Deshabilitado (legacy, reemplazado por CSP)
            // Valor "0" previene activación inconsistente en navegadores antiguos
            context.Response.Headers["X-XSS-Protection"] = "0";

            // Referrer-Policy: Controla qué información de referrer se envía
            // strict-origin-when-cross-origin: Envía origin completo solo en same-origin
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Permissions-Policy: Controla qué APIs del navegador pueden usarse
            // Deshabilita APIs no necesarias (cámara, micrófono, geolocalización, etc)
            context.Response.Headers["Permissions-Policy"] = 
                "camera=(), microphone=(), geolocation=(), payment=(), usb=()";

            // Content-Security-Policy: Política de seguridad de contenido
            // Define fuentes permitidas para scripts, estilos, imágenes, etc
            var csp = BuildContentSecurityPolicy();
            context.Response.Headers["Content-Security-Policy"] = csp;
        }

        return _next(context);
    }

    private static string BuildContentSecurityPolicy()
    {
        // CSP restrictivo para Blazor Server + MudBlazor
        var directives = new List<string>
        {
            // default-src: Fallback para directivas no especificadas
            "default-src 'self'",

            // script-src: Scripts permitidos
            // 'self': Scripts del mismo origen
            // 'unsafe-inline': Requerido por Blazor Server para scripts inline
            // 'unsafe-eval': Requerido por Blazor Server para eval()
            // https://www.googletagmanager.com: Google Analytics
            // https://static.cloudflareinsights.com: Cloudflare Analytics
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://www.googletagmanager.com https://static.cloudflareinsights.com",

            // style-src: Estilos permitidos
            // 'self': Estilos del mismo origen
            // 'unsafe-inline': Requerido por MudBlazor para estilos inline
            // https://fonts.googleapis.com: Google Fonts
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",

            // font-src: Fuentes permitidas
            // 'self': Fuentes del mismo origen
            // https://fonts.gstatic.com: Google Fonts
            "font-src 'self' https://fonts.gstatic.com",

            // img-src: Imágenes permitidas
            // 'self': Imágenes del mismo origen
            // data: Data URIs (usado por MudBlazor para iconos)
            // https://*.googleusercontent.com: Avatares de Google OAuth
            // https://*.licdn.com: Avatares de LinkedIn OAuth
            "img-src 'self' data: https://*.googleusercontent.com https://*.licdn.com",

            // connect-src: Conexiones AJAX/WebSocket permitidas
            // 'self': Conexiones al mismo origen
            // wss:: WebSocket (requerido por Blazor Server SignalR)
            // https://www.googletagmanager.com: Google Tag Manager (gtag.js)
            // https://*.google-analytics.com: Google Analytics (incluye region1, region2, etc.)
            // https://cloudflareinsights.com: Cloudflare Analytics
            "connect-src 'self' wss: https://www.googletagmanager.com https://*.google-analytics.com https://cloudflareinsights.com",

            // frame-ancestors: Quién puede embeber esta página en iframe
            // 'none': Nadie puede embeber (equivalente a X-Frame-Options: DENY)
            "frame-ancestors 'none'",

            // base-uri: URLs base permitidas
            // 'self': Solo el mismo origen
            "base-uri 'self'",

            // form-action: Destinos permitidos para formularios
            // 'self': Solo el mismo origen
            "form-action 'self'",

            // upgrade-insecure-requests: Auto-upgrade HTTP a HTTPS
            "upgrade-insecure-requests"
        };

        return string.Join("; ", directives);
    }
}

/// <summary>
/// Extension methods para registrar SecurityHeadersMiddleware en el pipeline.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Agrega SecurityHeadersMiddleware al pipeline de la aplicación.
    /// Debe llamarse ANTES de UseStaticFiles para asegurar que los headers se apliquen a todos los recursos.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
