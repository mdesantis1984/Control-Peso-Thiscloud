using System.Diagnostics;

namespace ControlPeso.Web.Middleware;

/// <summary>
/// Middleware para medir y loguear la duración de requests HTTP.
/// Identifica operaciones lentas que afectan la experiencia de usuario.
/// </summary>
internal sealed class RequestDurationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestDurationMiddleware> _logger;
    private readonly long _slowRequestThresholdMs;

    public RequestDurationMiddleware(
        RequestDelegate next,
        ILogger<RequestDurationMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;

        // Configurable threshold for "slow" requests (default: 1000ms = 1 second)
        _slowRequestThresholdMs = configuration.GetValue<long>("Logging:SlowRequestThresholdMs", 1000);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for static files and health checks to reduce noise
        var path = context.Request.Path.Value ?? string.Empty;
        if (ShouldSkipLogging(path))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        var method = context.Request.Method;
        var requestPath = context.Request.Path;

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var statusCode = context.Response.StatusCode;
            var durationMs = sw.ElapsedMilliseconds;

            // Log all requests with duration, but use different levels based on threshold
            if (durationMs >= _slowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "Slow request detected - Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, Duration: {DurationMs}ms",
                    method, requestPath, statusCode, durationMs);
            }
            else
            {
                _logger.LogInformation(
                    "Request completed - Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, Duration: {DurationMs}ms",
                    method, requestPath, statusCode, durationMs);
            }
        }
    }

    private static bool ShouldSkipLogging(string path)
    {
        // Skip logging for:
        // - Static files (CSS, JS, images, fonts)
        // - Framework internal calls (_blazor, _framework)
        // - Health checks
        return path.StartsWith("/_", StringComparison.Ordinal) ||
               path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/fonts/", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".map", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".woff", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
               path.Equals("/health", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Extension methods para registrar el middleware de duración de requests.
/// </summary>
internal static class RequestDurationMiddlewareExtensions
{
    /// <summary>
    /// Registra el middleware de tracking de duración de requests.
    /// Debe llamarse DESPUÉS de UseRouting y ANTES de UseEndpoints.
    /// </summary>
    public static IApplicationBuilder UseRequestDurationTracking(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestDurationMiddleware>();
    }
}
