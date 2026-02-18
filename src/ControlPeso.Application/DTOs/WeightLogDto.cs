using ControlPeso.Domain.Enums;

namespace ControlPeso.Application.DTOs;

/// <summary>
/// DTO de respuesta para un registro de peso.
/// Representa la conversión tipada de la entidad WeightLog scaffolded.
/// </summary>
public sealed record WeightLogDto
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required DateOnly Date { get; init; }
    public required TimeOnly Time { get; init; }

    /// <summary>
    /// Peso en kilogramos (almacenamiento normalizado).
    /// </summary>
    public required decimal Weight { get; init; }

    public required WeightUnit DisplayUnit { get; init; }
    public string? Note { get; init; }
    public required WeightTrend Trend { get; init; }
    public required DateTime CreatedAt { get; init; }
}
