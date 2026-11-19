namespace ControlPeso.Web.Middleware;

/// <summary>
/// Middleware global para captura y manejo de excepciones no controladas.
/// Loguea excepciones y redirige a página de error amigable.
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

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

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

            await HandleExceptionAsync(context, ex);
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
