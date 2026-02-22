using Microsoft.Extensions.Logging;

namespace ControlPeso.Application.Logging;

/// <summary>
/// Extension methods para agregar contexto estructurado a logs.
/// Permite categorización y filtrado de logs por tipo de operación.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Crea un scope de logging con categoría "Business" para operaciones de negocio.
    /// </summary>
    /// <param name="logger">Logger a extender.</param>
    /// <param name="operation">Nombre de la operación de negocio.</param>
    /// <returns>Scope desechable que debe usarse con 'using'.</returns>
    public static IDisposable? BeginBusinessScope(this ILogger logger, string operation)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["LogType"] = "Business",
            ["Operation"] = operation
        });
    }

    /// <summary>
    /// Crea un scope de logging con categoría "Infrastructure" para operaciones de infraestructura.
    /// </summary>
    /// <param name="logger">Logger a extender.</param>
    /// <param name="operation">Nombre de la operación de infraestructura.</param>
    /// <returns>Scope desechable que debe usarse con 'using'.</returns>
    public static IDisposable? BeginInfrastructureScope(this ILogger logger, string operation)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["LogType"] = "Infrastructure",
            ["Operation"] = operation
        });
    }

    /// <summary>
    /// Crea un scope de logging con categoría "Security" para operaciones de seguridad.
    /// </summary>
    /// <param name="logger">Logger a extender.</param>
    /// <param name="operation">Nombre de la operación de seguridad.</param>
    /// <returns>Scope desechable que debe usarse con 'using'.</returns>
    public static IDisposable? BeginSecurityScope(this ILogger logger, string operation)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["LogType"] = "Security",
            ["Operation"] = operation
        });
    }
}
