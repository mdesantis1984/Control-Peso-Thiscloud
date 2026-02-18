using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;

namespace ControlPeso.Application.Interfaces;

/// <summary>
/// Service interface for weight log operations
/// </summary>
public interface IWeightLogService
{
    // GET operations
    Task<WeightLogDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<WeightLogDto>> GetByUserAsync(Guid userId, WeightLogFilter filter, CancellationToken ct = default);

    // CUD operations
    Task<WeightLogDto> CreateAsync(CreateWeightLogDto dto, CancellationToken ct = default);
    Task<WeightLogDto> UpdateAsync(Guid id, UpdateWeightLogDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // Statistics
    Task<WeightStatsDto> GetStatsAsync(Guid userId, DateRange range, CancellationToken ct = default);
}
