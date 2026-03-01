using ControlPeso.Application.DTOs;
using ControlPeso.Domain.Entities;
using ControlPeso.Domain.Enums;

namespace ControlPeso.Application.Mapping;

/// <summary>
/// Mapper estático para conversiones entre entidad WeightLog scaffolded y DTOs.
/// Responsable de conversiones de tipos (string→Guid, string→DateOnly/TimeOnly, double→decimal, int→enum).
/// </summary>
public static class WeightLogMapper
{
    /// <summary>
    /// Convierte entidad scaffolded WeightLogs a WeightLogDto.
    /// </summary>
    public static WeightLogDto ToDto(WeightLogs entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new WeightLogDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Date = entity.Date,
            Time = entity.Time,
            Weight = entity.Weight,
            DisplayUnit = (WeightUnit)entity.DisplayUnit,
            Note = entity.Note,
            Trend = (WeightTrend)entity.Trend,
            CreatedAt = entity.CreatedAt
        };
    }

    /// <summary>
    /// Convierte CreateWeightLogDto a entidad WeightLogs para inserción.
    /// Genera nuevo Id y establece CreatedAt, calcula Trend como Neutral (se actualiza en servicio).
    /// </summary>
    public static WeightLogs ToEntity(CreateWeightLogDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var now = DateTime.UtcNow;

        return new WeightLogs
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            Date = dto.Date,
            Time = dto.Time,
            Weight = dto.Weight,
            DisplayUnit = (int)dto.DisplayUnit,
            Note = dto.Note,
            Trend = (int)WeightTrend.Neutral,
            CreatedAt = now
        };
    }

    /// <summary>
    /// Actualiza entidad WeightLogs existente con datos de UpdateWeightLogDto.
    /// NO modifica Id, UserId, CreatedAt.
    /// </summary>
    public static void UpdateEntity(WeightLogs entity, UpdateWeightLogDto dto)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(dto);

        entity.Date = dto.Date;
        entity.Time = dto.Time;
        entity.Weight = dto.Weight;
        entity.DisplayUnit = (int)dto.DisplayUnit;
        entity.Note = dto.Note;
        // Trend se recalcula en el servicio después de la actualización
    }
}
