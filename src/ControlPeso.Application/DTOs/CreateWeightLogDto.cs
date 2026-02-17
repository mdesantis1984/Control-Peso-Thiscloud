using ControlPeso.Domain.Enums;

namespace ControlPeso.Application.DTOs;

/// <summary>
/// DTO de entrada para crear un registro de peso.
/// </summary>
public sealed record CreateWeightLogDto
{
    public required Guid UserId { get; init; }
    public required DateOnly Date { get; init; }
    public required TimeOnly Time { get; init; }
    
    /// <summary>
    /// Peso en kilogramos (siempre normalizado a kg).
    /// Si el usuario ingresó en lb, la conversión debe hacerse ANTES de llamar al servicio.
    /// </summary>
    public required decimal Weight { get; init; }
    
    public required WeightUnit DisplayUnit { get; init; }
    public string? Note { get; init; }
}
