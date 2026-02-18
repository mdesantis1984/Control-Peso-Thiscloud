using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using AppDateRange = ControlPeso.Application.Filters.DateRange;

namespace ControlPeso.Web.Pages;

public partial class Trends
{
    [Inject] private IWeightLogService WeightLogService { get; set; } = null!;
    [Inject] private IUserService UserService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private ILogger<Trends> Logger { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private bool _isLoading = true;
    private List<WeightLogDto> _logs = [];

    // Métricas calculadas
    private decimal _currentWeight;
    private decimal? _startingWeight;
    private decimal _minWeight;
    private decimal _maxWeight;
    private decimal _overallAverage;
    private decimal _totalChange;

    // Tendencias por periodo
    private decimal _last7DaysAvg;
    private decimal _previous7DaysAvg;
    private decimal _last30DaysAvg;
    private decimal _previous30DaysAvg;

    // Proyección
    private decimal? _projectedWeight;
    private decimal _projectedChange;

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("Loading trends analysis");

        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var userIdClaim = authState.User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                Logger.LogWarning("User ID claim not found or invalid");
                Snackbar.Add("No se pudo identificar al usuario", Severity.Error);
                return;
            }

            // Cargar últimos 90 días de registros
            var filter = new WeightLogFilter
            {
                UserId = userId,
                Page = 1,
                PageSize = 1000,
                DateRange = new AppDateRange
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-90)),
                    EndDate = DateOnly.FromDateTime(DateTime.Today)
                }
            };

            var result = await WeightLogService.GetByUserAsync(userId, filter);
            _logs = result.Items.OrderBy(x => x.Date).ThenBy(x => x.Time).ToList();

            if (_logs.Count == 0)
            {
                Logger.LogInformation("No weight logs found for user {UserId}", userId);
                return;
            }

            // Cargar datos de usuario para peso inicial
            var user = await UserService.GetByIdAsync(userId);
            _startingWeight = user?.StartingWeight;

            // Calcular métricas
            CalculateMetrics();

            Logger.LogInformation("Trends analysis loaded successfully - {Count} records analyzed", _logs.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading trends analysis");
            Snackbar.Add("Error al cargar el análisis de tendencias", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void CalculateMetrics()
    {
        if (_logs.Count == 0) return;

        // Métricas básicas
        _currentWeight = _logs.Last().Weight;
        _minWeight = _logs.Min(x => x.Weight);
        _maxWeight = _logs.Max(x => x.Weight);
        _overallAverage = _logs.Average(x => x.Weight);
        _totalChange = _currentWeight - (_startingWeight ?? _logs.First().Weight);

        // Últimos 7 días vs anteriores 7 días
        var last7Days = _logs.Where(x => x.Date >= DateOnly.FromDateTime(DateTime.Today.AddDays(-7))).ToList();
        var previous7Days = _logs.Where(x => x.Date >= DateOnly.FromDateTime(DateTime.Today.AddDays(-14)) 
                                            && x.Date < DateOnly.FromDateTime(DateTime.Today.AddDays(-7))).ToList();

        _last7DaysAvg = last7Days.Count > 0 ? last7Days.Average(x => x.Weight) : _currentWeight;
        _previous7DaysAvg = previous7Days.Count > 0 ? previous7Days.Average(x => x.Weight) : _last7DaysAvg;

        // Últimos 30 días vs anteriores 30 días
        var last30Days = _logs.Where(x => x.Date >= DateOnly.FromDateTime(DateTime.Today.AddDays(-30))).ToList();
        var previous30Days = _logs.Where(x => x.Date >= DateOnly.FromDateTime(DateTime.Today.AddDays(-60)) 
                                             && x.Date < DateOnly.FromDateTime(DateTime.Today.AddDays(-30))).ToList();

        _last30DaysAvg = last30Days.Count > 0 ? last30Days.Average(x => x.Weight) : _currentWeight;
        _previous30DaysAvg = previous30Days.Count > 0 ? previous30Days.Average(x => x.Weight) : _last30DaysAvg;

        // Proyección simple (regresión lineal básica sobre últimos 30 días)
        if (last30Days.Count >= 5)
        {
            var daysSinceStart = (DateTime.Today - last30Days.First().Date.ToDateTime(TimeOnly.MinValue)).Days;
            if (daysSinceStart > 0)
            {
                var weightChange = last30Days.Last().Weight - last30Days.First().Weight;
                var dailyRate = weightChange / daysSinceStart;
                _projectedChange = dailyRate * 30;
                _projectedWeight = _currentWeight + _projectedChange;
            }
        }

        Logger.LogDebug("Metrics calculated - Current: {Current}kg, Avg: {Avg}kg, Projection: {Proj}kg", 
            _currentWeight, _overallAverage, _projectedWeight);
    }

    private Color GetProjectionColor()
    {
        if (!_projectedWeight.HasValue) return Color.Default;

        // Verde si la proyección es menor que el peso actual (bajando)
        // Naranja si es mayor (subiendo)
        return _projectedWeight < _currentWeight ? Color.Success : Color.Warning;
    }
}
