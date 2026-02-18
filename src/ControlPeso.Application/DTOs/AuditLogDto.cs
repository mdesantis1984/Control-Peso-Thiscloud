namespace ControlPeso.Application.DTOs;

/// <summary>
/// DTO de respuesta para registro de auditoría.
/// Usado en panel de administración para mostrar audit trail.
/// </summary>
public sealed record AuditLogDto
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string Action { get; init; }
    public required string EntityType { get; init; }
    public required Guid EntityId { get; init; }

    /// <summary>
    /// JSON snapshot del estado anterior (null si es creación).
    /// </summary>
    public string? OldValue { get; init; }

    /// <summary>
    /// JSON snapshot del estado nuevo (null si es eliminación).
    /// </summary>
    public string? NewValue { get; init; }

    public required DateTime CreatedAt { get; init; }
}
