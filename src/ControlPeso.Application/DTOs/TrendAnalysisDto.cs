using ControlPeso.Domain.Enums;

namespace ControlPeso.Application.DTOs;

/// <summary>
/// DTO con análisis de tendencia de peso para un usuario en un rango de fechas.
/// </summary>
public sealed record TrendAnalysisDto
{
    public required Guid UserId { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }

    /// <summary>
    /// Tendencia general: Up = ganando peso, Down = perdiendo peso, Neutral = estable.
    /// </summary>
    public required WeightTrend OverallTrend { get; init; }

    /// <summary>
    /// Cambio promedio diario (kg/día).
    /// Calculado como: (PesoFinal - PesoInicial) / DíasEnRango.
    /// </summary>
    public decimal? AverageDailyChange { get; init; }

    /// <summary>
    /// Cambio promedio semanal (kg/semana).
    /// </summary>
    public decimal? AverageWeeklyChange { get; init; }

    /// <summary>
    /// Puntos de datos para el gráfico de tendencia.
    /// Lista de (Fecha, Peso) ordenada cronológicamente.
    /// </summary>
    public IReadOnlyList<TrendDataPoint> DataPoints { get; init; } = [];
}

/// <summary>
/// Punto de dato para el gráfico de tendencia.
/// </summary>
public sealed record TrendDataPoint
{
    public required DateOnly Date { get; init; }
    public required decimal Weight { get; init; }
}
