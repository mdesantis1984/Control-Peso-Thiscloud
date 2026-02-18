using ControlPeso.Domain.Enums;

namespace ControlPeso.Application.DTOs;

/// <summary>
/// DTO de respuesta para un usuario.
/// Representa la conversión tipada de la entidad User scaffolded.
/// </summary>
public sealed record UserDto
{
    public required Guid Id { get; init; }
    public required string GoogleId { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required UserRole Role { get; init; }
    public string? AvatarUrl { get; init; }
    public required DateTime MemberSince { get; init; }

    /// <summary>
    /// Altura en centímetros (siempre cm).
    /// </summary>
    public required decimal Height { get; init; }

    public required UnitSystem UnitSystem { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public required string Language { get; init; }
    public required UserStatus Status { get; init; }

    /// <summary>
    /// Peso objetivo en kilogramos (siempre kg).
    /// </summary>
    public decimal? GoalWeight { get; init; }

    /// <summary>
    /// Peso inicial en kilogramos (siempre kg).
    /// </summary>
    public decimal? StartingWeight { get; init; }

    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
