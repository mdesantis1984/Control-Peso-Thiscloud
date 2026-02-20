using ControlPeso.Web.Services;
using Microsoft.Extensions.Logging;

namespace ControlPeso.Web.Middleware;

/// <summary>
/// Middleware global para captura y manejo de excepciones no controladas.
/// Loguea excepciones, envía notificaciones y redirige a página de error amigable.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(environment);

        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(
        HttpContext context,
        INotificationService notificationService)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(notificationService);

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception occurred - Path: {Path}, Method: {Method}, User: {User}, TraceId: {TraceId}",
                context.Request.Path,
                context.Request.Method,
                context.User?.Identity?.Name ?? "Anonymous",
                context.TraceIdentifier
            );

            // Send critical error notification (fire and forget - don't wait)
            _ = SendNotificationAsync(notificationService, context, ex);

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task SendNotificationAsync(
        INotificationService notificationService,
        HttpContext context,
        Exception exception)
    {
        try
        {
            var errorMessage = $"Path: {context.Request.Method} {context.Request.Path}\n" +
                               $"User: {context.User?.Identity?.Name ?? "Anonymous"}";

            await notificationService.SendCriticalErrorAsync(
                errorMessage,
                context.TraceIdentifier,
                exception,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Log failure but don't throw - notification failure should not crash the app
            _logger.LogError(ex,
                "Failed to send error notification - TraceId: {TraceId}",
                context.TraceIdentifier);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Prevenir re-write de response si ya se inició
        if (context.Response.HasStarted)
        {
            _logger.LogWarning(
                "Cannot handle exception - Response already started. TraceId: {TraceId}",
                context.TraceIdentifier
            );
            return Task.CompletedTask;
        }

        // Redirigir a página de error amigable con TraceId
        context.Response.Redirect($"/error?traceId={context.TraceIdentifier}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Extension methods para registrar GlobalExceptionMiddleware en el pipeline.
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    /// <summary>
    /// Agrega GlobalExceptionMiddleware al pipeline de la aplicación.
    /// Debe llamarse lo MÁS PRONTO posible en el pipeline para capturar todas las excepciones.
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
