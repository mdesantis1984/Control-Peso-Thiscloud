using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ControlPeso.Infrastructure.Data;

/// <summary>
/// Factory pública para crear instancias de IDbSeeder.
/// Permite a los tests crear seeders sin acceder directamente a la clase internal DbSeeder.
/// </summary>
public static class DbSeederFactory
{
    /// <summary>
    /// Crea una instancia de IDbSeeder para el contexto dado.
    /// </summary>
    /// <param name="context">DbContext a poblar con datos de seed.</param>
    /// <param name="logger">Logger para operaciones de seeding. Si es null, usa NullLogger.</param>
    /// <returns>Instancia de IDbSeeder lista para usar.</returns>
    public static IDbSeeder Create(ControlPesoDbContext context, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Usar NullLogger si no se proporciona logger
        var effectiveLogger = logger ?? NullLogger.Instance;

        // Crear logger tipado usando adapter interno
        var typedLogger = new LoggerAdapter(effectiveLogger);

        return new DbSeeder(context, typedLogger);
    }

    // Adapter privado para convertir ILogger a ILogger<DbSeeder>
    private sealed class LoggerAdapter : ILogger<DbSeeder>
    {
        private readonly ILogger _logger;

        public LoggerAdapter(ILogger logger) => _logger = logger;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => _logger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel)
            => _logger.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => _logger.Log(logLevel, eventId, state, exception, formatter);
    }
}
