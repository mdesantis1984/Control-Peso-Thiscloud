using ControlPeso.Domain.Enums;

namespace ControlPeso.Application.DTOs;

/// <summary>
/// DTO de entrada para actualizar el perfil de un usuario.
/// Solo campos editables por el usuario.
/// </summary>
public sealed record UpdateUserProfileDto
{
    public required string Name { get; init; }

    /// <summary>
    /// Altura en centímetros (siempre cm).
    /// </summary>
    public required decimal Height { get; init; }

    public required UnitSystem UnitSystem { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public required string Language { get; init; }

    /// <summary>
    /// Peso objetivo en kilogramos (siempre kg).
    /// </summary>
    public decimal? GoalWeight { get; init; }

    /// <summary>
    /// URL o ruta de la foto de perfil.
    /// </summary>
    public string? AvatarUrl { get; init; }
}
