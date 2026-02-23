using Microsoft.Extensions.Logging;

namespace ControlPeso.Web.Middleware;

/// <summary>
/// Middleware global para captura y manejo de excepciones no controladas.
/// Loguea excepciones con ILogger y retorna respuestas HTTP apropiadas según el environment.
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

        // Determinar status code según tipo de excepción
        var statusCode = exception switch
        {
            ArgumentNullException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            InvalidOperationException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/plain; charset=utf-8";

        // En Development: mostrar detalles de la excepción
        // En Production: mostrar mensaje genérico (no revelar stack traces)
        var message = _environment.IsDevelopment()
            ? BuildDevelopmentErrorMessage(exception, context.TraceIdentifier)
            : BuildProductionErrorMessage(statusCode, context.TraceIdentifier);

        return context.Response.WriteAsync(message);
    }

    private static string BuildDevelopmentErrorMessage(Exception exception, string traceId)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Error: {exception.Message}");
        sb.AppendLine();
        sb.AppendLine($"Type: {exception.GetType().FullName}");
        sb.AppendLine($"TraceId: {traceId}");
        sb.AppendLine();
        sb.AppendLine("Stack Trace:");
        sb.AppendLine(exception.StackTrace ?? "No stack trace available");

        if (exception.InnerException is not null)
        {
            sb.AppendLine();
            sb.AppendLine("Inner Exception:");
            sb.AppendLine($"  {exception.InnerException.Message}");
            sb.AppendLine($"  {exception.InnerException.StackTrace}");
        }

        return sb.ToString();
    }

    private static string BuildProductionErrorMessage(int statusCode, string traceId)
    {
        var message = statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request - The request was invalid.",
            StatusCodes.Status401Unauthorized => "Unauthorized - Please log in to access this resource.",
            StatusCodes.Status403Forbidden => "Forbidden - You do not have permission to access this resource.",
            StatusCodes.Status404NotFound => "Not Found - The requested resource was not found.",
            StatusCodes.Status409Conflict => "Conflict - The request could not be processed due to a conflict.",
            StatusCodes.Status500InternalServerError => "Internal Server Error - An unexpected error occurred.",
            _ => "An error occurred while processing your request."
        };

        return $"{message}\n\nTraceId: {traceId}\n\nPlease contact support if the problem persists.";
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
