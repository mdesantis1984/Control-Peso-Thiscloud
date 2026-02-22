using ControlPeso.Application.DTOs;
using ControlPeso.Infrastructure;
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
            Id = Guid.Parse(entity.Id),
            UserId = Guid.Parse(entity.UserId),
            Date = DateOnly.Parse(entity.Date),
            Time = TimeOnly.Parse(entity.Time),
            Weight = (decimal)entity.Weight,
            DisplayUnit = (WeightUnit)entity.DisplayUnit,
            Note = entity.Note,
            Trend = (WeightTrend)entity.Trend,
            CreatedAt = DateTime.Parse(entity.CreatedAt)
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
            Id = Guid.NewGuid().ToString(),
            UserId = dto.UserId.ToString(),
            Date = dto.Date.ToString("yyyy-MM-dd"),
            Time = dto.Time.ToString("HH:mm"),
            Weight = (double)dto.Weight,
            DisplayUnit = (int)dto.DisplayUnit,
            Note = dto.Note,
            Trend = (int)WeightTrend.Neutral,  // Default, se calcula en servicio
            CreatedAt = now.ToString("O")  // ISO 8601
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

        entity.Date = dto.Date.ToString("yyyy-MM-dd");
        entity.Time = dto.Time.ToString("HH:mm");
        entity.Weight = (double)dto.Weight;
        entity.DisplayUnit = (int)dto.DisplayUnit;
        entity.Note = dto.Note;
        // Trend se recalcula en el servicio después de la actualización
    }
}
