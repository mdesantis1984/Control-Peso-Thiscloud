namespace ControlPeso.Application.DTOs;

/// <summary>
/// DTO con estadísticas de peso para un usuario en un rango de fechas.
/// </summary>
public sealed record WeightStatsDto
{
    public required Guid UserId { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }

    /// <summary>
    /// Peso actual (más reciente en el rango).
    /// </summary>
    public decimal? CurrentWeight { get; init; }

    /// <summary>
    /// Peso inicial (más antiguo en el rango).
    /// </summary>
    public decimal? StartingWeight { get; init; }

    /// <summary>
    /// Peso promedio en el rango.
    /// </summary>
    public decimal? AverageWeight { get; init; }

    /// <summary>
    /// Peso máximo en el rango.
    /// </summary>
    public decimal? MaxWeight { get; init; }

    /// <summary>
    /// Peso mínimo en el rango.
    /// </summary>
    public decimal? MinWeight { get; init; }

    /// <summary>
    /// Cambio total (CurrentWeight - StartingWeight).
    /// Negativo = pérdida, Positivo = ganancia.
    /// </summary>
    public decimal? TotalChange { get; init; }

    /// <summary>
    /// Cantidad de registros en el rango.
    /// </summary>
    public int TotalRecords { get; init; }
}
