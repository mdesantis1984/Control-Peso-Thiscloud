using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Interfaces;
using ControlPeso.Application.Logging;
using ControlPeso.Application.Mapping;
using ControlPeso.Domain.Entities;
using ControlPeso.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ControlPeso.Application.Services;

/// <summary>
/// Servicio para gestión de registros de peso (WeightLogs).
/// Implementa operaciones CRUD y cálculo de estadísticas.
/// </summary>
public sealed class WeightLogService : IWeightLogService
{
    private readonly DbContext _context;
    private readonly ILogger<WeightLogService> _logger;

    public WeightLogService(
        DbContext context,
        ILogger<WeightLogService> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);

        _context = context;
        _logger = logger;
    }

    public async Task<WeightLogDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var scope = _logger.BeginBusinessScope("GetWeightLogById");
        _logger.LogInformation("Getting weight log by Id: {WeightLogId}", id);

        try
        {
            var entity = await _context.Set<WeightLogs>()
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == id, ct);

            if (entity is null)
            {
                _logger.LogWarning("Weight log not found: {WeightLogId}", id);
                return null;
            }

            var dto = WeightLogMapper.ToDto(entity);
            _logger.LogInformation("Weight log retrieved successfully: {WeightLogId}", id);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving weight log: {WeightLogId}", id);
            throw;
        }
    }

    public async Task<PagedResult<WeightLogDto>> GetByUserAsync(
        Guid userId,
        WeightLogFilter filter,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Getting weight logs for user {UserId} - Page: {Page}, PageSize: {PageSize}",
            userId, filter.Page, filter.PageSize);

        try
        {
            var query = _context.Set<WeightLogs>()
                .AsNoTracking()
                .Where(w => w.UserId == userId);

            // Apply date range filter if provided
            if (filter.DateRange is not null)
            {
                // DateOnly comparison in SQL Server
                query = query.Where(w =>
                    w.Date >= filter.DateRange.StartDate
                    && w.Date <= filter.DateRange.EndDate);
            }

            // Apply search term filter if provided (searches in Note field)
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchLower = filter.SearchTerm.Trim().ToLower();
                query = query.Where(w => w.Note != null && w.Note.ToLower().Contains(searchLower));
            }

            // Apply sorting
            query = filter.Descending
                ? query.OrderByDescending(w => w.Date).ThenByDescending(w => w.Time)
                : query.OrderBy(w => w.Date).ThenBy(w => w.Time);

            // Get total count
            var totalItems = await query.CountAsync(ct);

            // Apply pagination
            var entities = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(ct);

            var dtos = entities.Select(WeightLogMapper.ToDto).ToList();

            var result = new PagedResult<WeightLogDto>
            {
                Items = dtos,
                TotalCount = totalItems,
                Page = filter.Page,
                PageSize = filter.PageSize
            };

            _logger.LogInformation(
                "Retrieved {Count} weight logs for user {UserId} (Total: {Total})",
                dtos.Count, userId, totalItems);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving weight logs for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<WeightLogDto> CreateAsync(CreateWeightLogDto dto, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Creating weight log for user {UserId} - Date: {Date}, Weight: {Weight}kg",
            dto.UserId, dto.Date, dto.Weight);

        try
        {
            // Calculate trend based on previous weight
            var previousWeight = await GetLastWeightAsync(dto.UserId, dto.Date, ct);
            var trend = CalculateTrend(dto.Weight, previousWeight);

            var entity = WeightLogMapper.ToEntity(dto);
            entity.Trend = (int)trend;

            _context.Set<WeightLogs>().Add(entity);
            await _context.SaveChangesAsync(ct);

            // Update user's StartingWeight if this is the first log
            await UpdateUserStartingWeightIfNeededAsync(dto.UserId, entity.Weight, ct);

            var result = WeightLogMapper.ToDto(entity);

            _logger.LogInformation(
                "Weight log created successfully - Id: {WeightLogId}, UserId: {UserId}, Trend: {Trend}",
                result.Id, dto.UserId, trend);

            return result;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating weight log for user {UserId}", dto.UserId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating weight log for user {UserId}", dto.UserId);
            throw;
        }
    }

    public async Task<WeightLogDto> UpdateAsync(Guid id, UpdateWeightLogDto dto, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating weight log {WeightLogId}", id);

        try
        {
            var entity = await _context.Set<WeightLogs>()
                .FirstOrDefaultAsync(w => w.Id == id, ct);

            if (entity is null)
            {
                _logger.LogWarning("Weight log not found for update: {WeightLogId}", id);
                throw new InvalidOperationException($"Weight log with ID {id} not found.");
            }

            var userId = entity.UserId;

            WeightLogMapper.UpdateEntity(entity, dto);

            // Recalculate trend if weight changed
            var previousWeight = await GetLastWeightAsync(userId, dto.Date, ct);
            entity.Trend = (int)CalculateTrend(dto.Weight, previousWeight);

            await _context.SaveChangesAsync(ct);

            var result = WeightLogMapper.ToDto(entity);

            _logger.LogInformation("Weight log updated successfully: {WeightLogId}", id);

            return result;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error updating weight log {WeightLogId}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating weight log {WeightLogId}", id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting weight log {WeightLogId}", id);

        try
        {
            var entity = await _context.Set<WeightLogs>()
                .FirstOrDefaultAsync(w => w.Id == id, ct);

            if (entity is null)
            {
                _logger.LogWarning("Weight log not found for deletion: {WeightLogId}", id);
                throw new InvalidOperationException($"Weight log with ID {id} not found.");
            }

            _context.Set<WeightLogs>().Remove(entity);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Weight log deleted successfully: {WeightLogId}", id);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error deleting weight log {WeightLogId}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting weight log {WeightLogId}", id);
            throw;
        }
    }

    public async Task<WeightStatsDto> GetStatsAsync(Guid userId, DateRange range, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Getting weight stats for user {UserId} - Range: {StartDate} to {EndDate}",
            userId, range.StartDate, range.EndDate);

        try
        {
            // DateOnly comparison in SQL Server
            var weights = await _context.Set<WeightLogs>()
                .AsNoTracking()
                .Where(w => w.UserId == userId
                    && w.Date >= range.StartDate
                    && w.Date <= range.EndDate)
                .Select(w => w.Weight)
                .ToListAsync(ct);

            if (weights.Count == 0)
            {
                _logger.LogWarning("No weight logs found for stats calculation: {UserId}", userId);
                return new WeightStatsDto
                {
                    UserId = userId,
                    StartDate = range.StartDate,
                    EndDate = range.EndDate,
                    CurrentWeight = null,
                    StartingWeight = null,
                    AverageWeight = null,
                    MinWeight = null,
                    MaxWeight = null,
                    TotalChange = null,
                    TotalRecords = 0
                };
            }

            var currentWeight = weights[^1];
            var firstWeight = weights[0];
            var avgWeight = weights.Average();
            var minWeight = weights.Min();
            var maxWeight = weights.Max();
            var weightChange = currentWeight - firstWeight;

            var stats = new WeightStatsDto
            {
                UserId = userId,
                StartDate = range.StartDate,
                EndDate = range.EndDate,
                CurrentWeight = (decimal)currentWeight,
                StartingWeight = (decimal)firstWeight,
                AverageWeight = (decimal)avgWeight,
                MinWeight = (decimal)minWeight,
                MaxWeight = (decimal)maxWeight,
                TotalChange = (decimal)weightChange,
                TotalRecords = weights.Count
            };

            _logger.LogInformation(
                "Stats calculated for user {UserId} - Current: {Current}kg, Avg: {Average}kg, Change: {Change}kg",
                userId, currentWeight, avgWeight, weightChange);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating stats for user {UserId}", userId);
            throw;
        }
    }

    // Private helper methods

    private async Task<decimal?> GetLastWeightAsync(Guid userId, DateOnly beforeDate, CancellationToken ct)
    {
        // DateOnly comparison in SQL Server
        var lastWeight = await _context.Set<WeightLogs>()
            .AsNoTracking()
            .Where(w => w.UserId == userId
                && w.Date < beforeDate)
            .OrderByDescending(w => w.Date)
            .ThenByDescending(w => w.Time)
            .Select(w => (decimal?)w.Weight)
            .FirstOrDefaultAsync(ct);

        return lastWeight;
    }

    private static WeightTrend CalculateTrend(decimal currentWeight, decimal? previousWeight)
    {
        if (!previousWeight.HasValue)
            return WeightTrend.Neutral;

        var diff = currentWeight - previousWeight.Value;

        return diff switch
        {
            > 0.1m => WeightTrend.Up,
            < -0.1m => WeightTrend.Down,
            _ => WeightTrend.Neutral
        };
    }

    private async Task UpdateUserStartingWeightIfNeededAsync(Guid userId, decimal weight, CancellationToken ct)
    {
        var user = await _context.Set<Users>()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is not null && !user.StartingWeight.HasValue)
        {
            user.StartingWeight = weight;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Set starting weight for user {UserId}: {Weight}kg",
                userId, weight);
        }
    }
}
