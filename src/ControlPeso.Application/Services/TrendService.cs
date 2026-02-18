using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Entities;
using ControlPeso.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ControlPeso.Application.Services;

/// <summary>
/// Servicio para análisis de tendencias y proyecciones de peso.
/// Implementa cálculos estadísticos y regresión lineal simple.
/// </summary>
public sealed class TrendService : ITrendService
{
    private readonly DbContext _context;
    private readonly ILogger<TrendService> _logger;

    // Constantes para cálculos
    private const decimal WeightThreshold = 0.1m; // 100g de tolerancia para considerar "neutral"
    private const int DefaultProjectionDays = 30; // Proyección por defecto a 30 días

    public TrendService(
        DbContext context,
        ILogger<TrendService> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);

        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene análisis de tendencia para un usuario en un rango de fechas.
    /// Calcula tendencia general, cambios promedios y devuelve puntos de datos para gráfico.
    /// </summary>
    public async Task<TrendAnalysisDto> GetTrendAnalysisAsync(Guid userId, DateRange range, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(range);

        _logger.LogInformation(
            "Getting trend analysis for user: {UserId} - Start: {StartDate}, End: {EndDate}",
            userId, range.StartDate, range.EndDate);

        try
        {
            // Obtener registros de peso en el rango ordenados cronológicamente
            var logs = await _context.Set<WeightLogs>()
                .AsNoTracking()
                .Where(wl => wl.UserId == userId.ToString())
                .Where(wl => string.Compare(wl.Date, range.StartDate.ToString("yyyy-MM-dd")) >= 0 &&
                             string.Compare(wl.Date, range.EndDate.ToString("yyyy-MM-dd")) <= 0)
                .OrderBy(wl => wl.Date)
                .ThenBy(wl => wl.Time)
                .ToListAsync(ct);

            if (logs.Count == 0)
            {
                _logger.LogWarning("No weight logs found for user {UserId} in range {StartDate} to {EndDate}",
                    userId, range.StartDate, range.EndDate);

                return new TrendAnalysisDto
                {
                    UserId = userId,
                    StartDate = range.StartDate,
                    EndDate = range.EndDate,
                    OverallTrend = WeightTrend.Neutral,
                    DataPoints = []
                };
            }

            // Convertir a puntos de datos para el gráfico
            var dataPoints = logs
                .Select(log => new TrendDataPoint
                {
                    Date = DateOnly.Parse(log.Date),
                    Weight = (decimal)log.Weight
                })
                .ToList();

            // Calcular tendencia general (comparar primer vs último registro)
            var firstWeight = (decimal)logs.First().Weight;
            var lastWeight = (decimal)logs.Last().Weight;
            var weightChange = lastWeight - firstWeight;

            var overallTrend = Math.Abs(weightChange) <= WeightThreshold
                ? WeightTrend.Neutral
                : weightChange > 0
                    ? WeightTrend.Up
                    : WeightTrend.Down;

            // Calcular cambio promedio diario y semanal
            var firstDate = DateOnly.Parse(logs.First().Date);
            var lastDate = DateOnly.Parse(logs.Last().Date);
            var daysInRange = (lastDate.ToDateTime(TimeOnly.MinValue) - firstDate.ToDateTime(TimeOnly.MinValue)).Days;

            decimal? averageDailyChange = null;
            decimal? averageWeeklyChange = null;

            if (daysInRange > 0 && Math.Abs(weightChange) > WeightThreshold)
            {
                averageDailyChange = Math.Round(weightChange / daysInRange, 3);
                averageWeeklyChange = Math.Round(averageDailyChange.Value * 7, 3);
            }

            var result = new TrendAnalysisDto
            {
                UserId = userId,
                StartDate = range.StartDate,
                EndDate = range.EndDate,
                OverallTrend = overallTrend,
                AverageDailyChange = averageDailyChange,
                AverageWeeklyChange = averageWeeklyChange,
                DataPoints = dataPoints
            };

            _logger.LogInformation(
                "Trend analysis completed for user {UserId} - Logs: {LogCount}, Trend: {Trend}, DailyChange: {DailyChange}kg, WeeklyChange: {WeeklyChange}kg",
                userId, logs.Count, overallTrend, averageDailyChange, averageWeeklyChange);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting trend analysis for user {UserId} in range {StartDate} to {EndDate}",
                userId, range.StartDate, range.EndDate);
            throw;
        }
    }

    /// <summary>
    /// Obtiene proyección de peso futuro basada en tendencia histórica.
    /// Usa regresión lineal simple sobre los últimos 30 días de datos.
    /// </summary>
    public async Task<WeightProjectionDto> GetProjectionAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting weight projection for user: {UserId}", userId);

        try
        {
            // Obtener usuario para verificar peso objetivo
            var user = await _context.Set<Users>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.ToString(), ct);

            if (user is null)
            {
                _logger.LogWarning("User not found for projection: {UserId}", userId);
                throw new InvalidOperationException($"User with ID {userId} not found.");
            }

            var goalWeight = user.GoalWeight.HasValue ? (decimal)user.GoalWeight.Value : (decimal?)null;

            // Obtener registros de peso de los últimos 30 días para la regresión
            var thirtyDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var logs = await _context.Set<WeightLogs>()
                .AsNoTracking()
                .Where(wl => wl.UserId == userId.ToString())
                .Where(wl => string.Compare(wl.Date, thirtyDaysAgo.ToString("yyyy-MM-dd")) >= 0 &&
                             string.Compare(wl.Date, today.ToString("yyyy-MM-dd")) <= 0)
                .OrderBy(wl => wl.Date)
                .ThenBy(wl => wl.Time)
                .ToListAsync(ct);

            // Si no hay suficientes datos, devolver proyección sin datos
            if (logs.Count < 2)
            {
                _logger.LogWarning("Insufficient data for projection - User: {UserId}, LogCount: {LogCount}",
                    userId, logs.Count);

                return new WeightProjectionDto
                {
                    UserId = userId,
                    ProjectionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(DefaultProjectionDays)),
                    GoalWeight = goalWeight,
                    IsOnTrack = false
                };
            }

            // Calcular regresión lineal simple: y = mx + b
            // Donde x = días desde el primer registro, y = peso
            var firstDate = DateOnly.Parse(logs.First().Date);

            var dataPoints = logs
                .Select(log => new
                {
                    X = (DateOnly.Parse(log.Date).ToDateTime(TimeOnly.MinValue) - firstDate.ToDateTime(TimeOnly.MinValue)).Days,
                    Y = (decimal)log.Weight
                })
                .ToList();

            var n = dataPoints.Count;
            var sumX = dataPoints.Sum(p => p.X);
            var sumY = dataPoints.Sum(p => p.Y);
            var sumXY = dataPoints.Sum(p => p.X * p.Y);
            var sumX2 = dataPoints.Sum(p => p.X * p.X);

            // Pendiente (m) y ordenada (b)
            var m = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            var b = (sumY - m * sumX) / n;

            _logger.LogDebug("Linear regression calculated - Slope: {Slope}, Intercept: {Intercept}", m, b);

            // Proyectar a 30 días desde hoy
            var projectionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(DefaultProjectionDays));
            var daysFromFirstLog = (projectionDate.ToDateTime(TimeOnly.MinValue) - firstDate.ToDateTime(TimeOnly.MinValue)).Days;
            var projectedWeight = Math.Round(m * daysFromFirstLog + b, 2);

            // Calcular si está en camino al objetivo
            DateOnly? estimatedGoalDate = null;
            var isOnTrack = false;

            if (goalWeight.HasValue && Math.Abs(m) > 0.001m) // Si hay objetivo y tendencia significativa
            {
                // Calcular cuántos días para alcanzar el objetivo
                var daysToGoal = (goalWeight.Value - b) / m;

                if (daysToGoal > 0 && daysToGoal < 365) // Entre 0 y 1 año
                {
                    estimatedGoalDate = firstDate.AddDays((int)Math.Round(daysToGoal));
                    isOnTrack = true;
                }
            }

            var result = new WeightProjectionDto
            {
                UserId = userId,
                ProjectionDate = projectionDate,
                ProjectedWeight = projectedWeight,
                GoalWeight = goalWeight,
                EstimatedGoalDate = estimatedGoalDate,
                IsOnTrack = isOnTrack
            };

            _logger.LogInformation(
                "Weight projection completed for user {UserId} - ProjectedWeight: {ProjectedWeight}kg on {ProjectionDate}, OnTrack: {OnTrack}",
                userId, projectedWeight, projectionDate, isOnTrack);

            return result;
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw user not found exception
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weight projection for user {UserId}", userId);
            throw;
        }
    }
}
