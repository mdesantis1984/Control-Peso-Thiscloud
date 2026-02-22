using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Interfaces;
using ControlPeso.Web.Components.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Security.Claims;

namespace ControlPeso.Web.Pages;

/// <summary>
/// Dashboard - Página principal con métricas y resumen
/// Muestra peso actual, cambio semanal, progreso hacia meta, gráfico de evolución,
/// registros recientes y estadísticas
/// </summary>
public partial class Dashboard
{
    [Inject] private IWeightLogService WeightLogService { get; set; } = null!;
    [Inject] private IUserService UserService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private ILogger<Dashboard> Logger { get; set; } = null!;

    private decimal _currentWeight = 0;
    private decimal _weeklyChange = 0;
    private decimal? _goalWeight = 0;
    private decimal _startingWeight = 0;
    private decimal _progress = 0;

    private List<WeightLogDto> _weightLogs = new();
    private List<WeightLogDto> _filteredWeightLogs = new();
    private List<WeightLogDto> _filteredTableLogs = new();
    private List<WeightLogDto> _recentLogs = new();
    private WeightStatsDto? _stats = null;

    private Guid _currentUserId;
    private string _userName = string.Empty;
    private bool _isLoading = true;

    // Period selector state
    private int _selectedPeriod = 30; // Default: 1M

    // Search functionality
    private string _searchString = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("Dashboard: Initializing");

        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();

            // Verificar si el usuario está autenticado
            if (!authState.User.Identity?.IsAuthenticated ?? true)
            {
                Logger.LogWarning("Dashboard: User is not authenticated - Redirecting to login");
                Navigation.NavigateTo("/login", forceLoad: true);
                return;
            }

            var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out _currentUserId))
            {
                Logger.LogWarning("Dashboard: Invalid or missing user ID claim - Redirecting to login");
                Navigation.NavigateTo("/login", forceLoad: true);
                return;
            }

            Logger.LogInformation("Dashboard: Loading data for user {UserId}", _currentUserId);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Dashboard: Error initializing");
            Snackbar.Add("Error al cargar el dashboard. Por favor, intenta nuevamente.", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            Logger.LogInformation("Dashboard: Loading data for user {UserId}", _currentUserId);

            // Obtener registros de los últimos 30 días
            var filter = new WeightLogFilter
            {
                UserId = _currentUserId,
                Page = 1,
                PageSize = 30
            };

            var result = await WeightLogService.GetByUserAsync(_currentUserId, filter);
            _weightLogs = result.Items.ToList();
            _recentLogs = _weightLogs.Take(5).ToList();

            Logger.LogInformation("Dashboard: Loaded {Count} weight logs", _weightLogs.Count);

            // Calcular métricas
            if (_weightLogs.Count != 0)
            {
                _currentWeight = _weightLogs.First().Weight;

                // Cambio semanal
                var lastWeek = _weightLogs
                    .Where(l => l.Date >= DateOnly.FromDateTime(DateTime.Now.AddDays(-7)))
                    .OrderBy(l => l.Date)
                    .ToList();

                if (lastWeek.Count >= 2)
                {
                    _weeklyChange = _weightLogs.First().Weight - lastWeek.First().Weight;
                    Logger.LogDebug("Dashboard: Weekly change calculated - {Change}kg", _weeklyChange);
                }
            }

            // Obtener datos del usuario y calcular progreso
            var user = await UserService.GetByIdAsync(_currentUserId);
            if (user != null)
            {
                _userName = user.Name;
                _goalWeight = user.GoalWeight;
                _startingWeight = user.StartingWeight ?? 0;

                if (_goalWeight.HasValue && user.StartingWeight.HasValue && _currentWeight > 0)
                {
                    var totalToLose = user.StartingWeight.Value - _goalWeight.Value;
                    var lostSoFar = user.StartingWeight.Value - _currentWeight;
                    _progress = totalToLose != 0 ? (lostSoFar / totalToLose) * 100 : 0;

                    Logger.LogDebug("Dashboard: Progress calculated - {Progress}%", _progress);
                }
            }

            // Aplicar filtros iniciales
            _filteredWeightLogs = FilterByPeriod(_selectedPeriod);
            _filteredTableLogs = _weightLogs; // Inicialmente sin filtrar

            // Obtener estadísticas
            var dateRange = new Application.Filters.DateRange
            {
                StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
                EndDate = DateOnly.FromDateTime(DateTime.Now)
            };

            _stats = await WeightLogService.GetStatsAsync(_currentUserId, dateRange);

            Logger.LogInformation("Dashboard: Data loaded successfully - CurrentWeight: {Weight}kg, WeeklyChange: {Change}kg, Progress: {Progress}%",
                _currentWeight, _weeklyChange, _progress);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Dashboard: Error loading data for user {UserId}", _currentUserId);
            Snackbar.Add($"Error al cargar datos: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// Cambia el período de visualización del gráfico
    /// </summary>
    private void ChangePeriod(int days)
    {
        Logger.LogDebug("Dashboard: Changing period to {Days} days", days);
        _selectedPeriod = days;
        _filteredWeightLogs = FilterByPeriod(days);
        StateHasChanged();
    }

    /// <summary>
    /// Filtra los registros de peso por período en días
    /// </summary>
    private List<WeightLogDto> FilterByPeriod(int days)
    {
        var startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-days));
        return _weightLogs.Where(l => l.Date >= startDate).ToList();
    }

    /// <summary>
    /// Obtiene el texto formateado para "Last measured"
    /// </summary>
    private string GetLastMeasuredText()
    {
        if (_weightLogs.Count == 0)
            return "N/A";

        var lastLog = _weightLogs.First();
        var lastDateTime = lastLog.Date.ToDateTime(lastLog.Time);
        var diff = DateTime.Now - lastDateTime;

        if (diff.TotalHours < 1)
            return "a few minutes ago";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours} hours ago";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays} days ago";

        return lastLog.Date.ToString("MMM dd, yyyy");
    }

    /// <summary>
    /// Obtiene el peso restante para alcanzar la meta
    /// </summary>
    private string GetRemainingWeight()
    {
        if (!_goalWeight.HasValue || _currentWeight == 0)
            return "N/A";

        var remaining = _currentWeight - _goalWeight.Value;
        return Math.Abs(remaining).ToString("F1");
    }

    /// <summary>
    /// Exporta los datos a CSV
    /// </summary>
    private void ExportData()
    {
        Logger.LogInformation("Dashboard: Exporting data for user {UserId}", _currentUserId);
        Snackbar.Add("Función de exportación próximamente disponible", Severity.Info);
    }

    private async Task OpenAddWeightDialog()
    {
        Logger.LogInformation("Dashboard: Opening AddWeightDialog for user {UserId}", _currentUserId);

        var dialog = await DialogService.ShowAsync<AddWeightDialog>("Registrar Peso");
        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            Logger.LogInformation("Dashboard: Weight added successfully, reloading data");
            Snackbar.Add("Peso registrado correctamente", Severity.Success);
            await LoadDataAsync();
            StateHasChanged();
        }
        else
        {
            Logger.LogDebug("Dashboard: AddWeightDialog canceled");
        }
    }
}
