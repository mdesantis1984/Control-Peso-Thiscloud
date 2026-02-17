using ControlPeso.Domain.Enums;

namespace ControlPeso.Application.DTOs;

/// <summary>
/// DTO de entrada para actualizar un registro de peso existente.
/// </summary>
public sealed record UpdateWeightLogDto
{
    public required DateOnly Date { get; init; }
    public required TimeOnly Time { get; init; }
    
    /// <summary>
    /// Peso en kilogramos (siempre normalizado a kg).
    /// </summary>
    public required decimal Weight { get; init; }
    
    public required WeightUnit DisplayUnit { get; init; }
    public string? Note { get; init; }
}
