namespace ControlPeso.Application.DTOs;

/// <summary>
/// DTO con proyección de peso futuro basada en tendencia histórica.
/// </summary>
public sealed record WeightProjectionDto
{
    public required Guid UserId { get; init; }

    /// <summary>
    /// Fecha de la proyección (ej: +30 días desde hoy).
    /// </summary>
    public required DateOnly ProjectionDate { get; init; }

    /// <summary>
    /// Peso proyectado en kg (basado en regresión lineal simple).
    /// </summary>
    public decimal? ProjectedWeight { get; init; }

    /// <summary>
    /// Peso objetivo del usuario (si está configurado).
    /// </summary>
    public decimal? GoalWeight { get; init; }

    /// <summary>
    /// Fecha estimada para alcanzar el peso objetivo (si es alcanzable con tendencia actual).
    /// </summary>
    public DateOnly? EstimatedGoalDate { get; init; }

    /// <summary>
    /// Indica si la tendencia actual lleva al objetivo.
    /// </summary>
    public bool IsOnTrack { get; init; }
}
