using ControlPeso.Application.DTOs;
using ControlPeso.Domain.Entities;

namespace ControlPeso.Application.Mapping;

/// <summary>
/// Mapper estático para entidad AuditLog.
/// Usado por admin panel para mostrar audit trail.
/// </summary>
public static class AuditLogMapper
{
    /// <summary>
    /// Convierte entidad scaffolded AuditLog a DTO (si necesario para admin panel).
    /// </summary>
    public static AuditLogDto ToDto(AuditLog entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        return new AuditLogDto
        {
            Id = Guid.Parse(entity.Id),
            UserId = Guid.Parse(entity.UserId),
            Action = entity.Action,
            EntityType = entity.EntityType,
            EntityId = Guid.Parse(entity.EntityId),
            OldValue = entity.OldValue,
            NewValue = entity.NewValue,
            CreatedAt = DateTime.Parse(entity.CreatedAt)
        };
    }
    
    /// <summary>
    /// Crea entidad AuditLog para registrar un cambio administrativo.
    /// </summary>
    public static AuditLog CreateEntity(
        Guid userId,
        string action,
        string entityType,
        Guid entityId,
        string? oldValue,
        string? newValue)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(entityType);
        
        return new AuditLog
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.ToString(),
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            OldValue = oldValue,
            NewValue = newValue,
            CreatedAt = DateTime.UtcNow.ToString("O")
        };
    }
}
