using System.Globalization;
using System.Security.Claims;
using System.Text;
using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Enums;
using ControlPeso.Web.Components.Shared;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;

namespace ControlPeso.Web.Pages;

/// <summary>
/// Dashboard - Página principal con métricas y resumen
/// Muestra peso actual, cambio semanal, progreso hacia meta, gráfico de evolución,
/// registros recientes y estadísticas
/// </summary>
public partial class Dashboard : IDisposable
{
    [Inject] private IStringLocalizer<Dashboard> Localizer { get; set; } = null!;
    [Inject] private IWeightLogService WeightLogService { get; set; } = null!;
    [Inject] private IUserService UserService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private Services.NotificationService Snackbar { get; set; } = null!; // User notification service con verificación de preferencias
    [Inject] private Services.UserStateService UserStateService { get; set; } = null!; // ✅ Global Unit System state
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
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
    private bool _isExporting = false;

    // Period selector state
    private int _selectedPeriod = 30; // Default: 1M

    // Search functionality
    private string _searchString = string.Empty;

    // ========================================================================
    // UNIT CONVERSION HELPERS
    // ========================================================================

    /// <summary>
    /// Converts weight from kg (storage format) to user's preferred unit (kg or lb).
    /// </summary>
    private decimal ConvertedWeight(decimal weightInKg) => UserStateService.ConvertWeight(weightInKg);

    /// <summary>
    /// Gets the weight unit label based on user's preference (kg or lb).
    /// </summary>
    private string WeightUnit => UserStateService.GetWeightUnitLabel();

    // ========================================================================
    // LOCALIZED PROPERTIES
    // ========================================================================

    // Page metadata
    private string PageTitle => Localizer["PageTitle"];
    private string MetaDescription => Localizer["MetaDescription"];
    private string MetaKeywords => Localizer["MetaKeywords"];
    private string OgTitle => Localizer["OgTitle"];
    private string OgDescription => Localizer["OgDescription"];

    // Empty state
    private string WelcomeTitle => Localizer["WelcomeTitle"];
    private string WelcomeSubtitle => Localizer["WelcomeSubtitle"];
    private string WelcomeDescription => Localizer["WelcomeDescription"];
    private string AddFirstWeightButton => Localizer["AddFirstWeightButton"];

    // Header section
    private string WelcomeBack => Localizer["WelcomeBack", _userName];
    private string ProgressSubtitle => Localizer["ProgressSubtitle"];
    private string ExportButton => Localizer["ExportButton"];

    // Stats cards
    private string CurrentWeightLabel => Localizer["CurrentWeightLabel"];
    private string LastMeasured => Localizer["LastMeasured", GetLastMeasuredText()];
    private string WeeklyChangeLabel => Localizer["WeeklyChangeLabel"];
    private string OnTrack => Localizer["OnTrack"];
    private string AboveTarget => Localizer["AboveTarget"];
    private string GoalProgressLabel => Localizer["GoalProgressLabel"];
    private string ToTarget => Localizer["ToTarget", GetRemainingWeight()];

    // Chart section
    private string WeightEvolutionTitle => Localizer["WeightEvolutionTitle"];
    private string WeightEvolutionSubtitle => Localizer["WeightEvolutionSubtitle"];
    private string Period1W => Localizer["Period1W"];
    private string Period1M => Localizer["Period1M"];
    private string Period3M => Localizer["Period3M"];
    private string PeriodAll => Localizer["PeriodAll"];

    // Table section
    private string MeasurementLogTitle => Localizer["MeasurementLogTitle"];
    private string SearchPlaceholder => Localizer["SearchPlaceholder"];
    private string TableHeaderDateTime => Localizer["TableHeaderDateTime"];
    private string TableHeaderWeight => Localizer["TableHeaderWeight"];
    private string TableHeaderTrend => Localizer["TableHeaderTrend"];
    private string TableHeaderNotes => Localizer["TableHeaderNotes"];
    private string TableHeaderActions => Localizer["TableHeaderActions"];
    private string PagerShowing => Localizer["PagerShowing", Math.Min(_filteredTableLogs.Count, 1), Math.Min(_filteredTableLogs.Count, 5), _filteredTableLogs.Count];

    // Accessibility
    private string FabAriaLabel => Localizer["FabAriaLabel"];
    private string MoreActionsAriaLabel => Localizer["MoreActionsAriaLabel"];

    // Error messages
    private string ErrorLoadingDashboard => Localizer["ErrorLoadingDashboard"];
    private string ErrorExportingData => Localizer["ErrorExportingData"];

    // Export messages
    private string NoDataToExport => Localizer["NoDataToExport"];
    private string ExportSuccess(int count) => Localizer["ExportSuccess", count];
    private string ErrorLoadingData(string details) => Localizer["ErrorLoadingData", details];

    // Success/info messages
    private string ExportComingSoon => Localizer["ExportComingSoon"];
    private string AddWeightDialogTitle => Localizer["AddWeightDialogTitle"];
    // WeightSavedSuccess property removed - notification now shown only in AddWeightDialog to avoid duplication

    // Relative time
    private string TimeAgoMinutes => Localizer["TimeAgoMinutes"];
    private string TimeAgoHours(int hours) => Localizer["TimeAgoHours", hours];
    private string TimeAgoDays(int days) => Localizer["TimeAgoDays", days];

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

            // Subscribe to unit system changes
            UserStateService.UserUnitSystemUpdated += OnUnitSystemChanged;

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Dashboard: Error initializing");
            Snackbar.Add(ErrorLoadingDashboard, Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Handler for UnitSystem changes - refreshes display when user changes preferences.
    /// </summary>
    private async void OnUnitSystemChanged(object? sender, Domain.Enums.UnitSystem newUnitSystem)
    {
        try
        {
            Logger.LogInformation("Dashboard: Unit system changed to {UnitSystem} - refreshing display", newUnitSystem);
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Dashboard: Error handling unit system change");
        }
    }

    public void Dispose()
    {
        // Unsubscribe from events to prevent memory leaks
        UserStateService.UserUnitSystemUpdated -= OnUnitSystemChanged;
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
            Snackbar.Add(ErrorLoadingData(ex.Message), Severity.Error);
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
            return TimeAgoMinutes;
        if (diff.TotalHours < 24)
            return TimeAgoHours((int)diff.TotalHours);
        if (diff.TotalDays < 7)
            return TimeAgoDays((int)diff.TotalDays);

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
        return $"{ConvertedWeight(Math.Abs(remaining)).ToString("F1")} {WeightUnit}";
    }

    /// <summary>
    /// Exporta los registros de peso del usuario a CSV
    /// </summary>
    private async Task ExportData()
    {
        if (_isExporting)
            return;

        _isExporting = true;

        try
        {
            Logger.LogInformation("Dashboard: Exporting weight logs to CSV for user {UserId}", _currentUserId);

            // Verificar si hay datos
            if (_weightLogs.Count == 0)
            {
                Snackbar.Add(NoDataToExport, Severity.Warning);
                return;
            }

            // Usar todos los registros sin filtrar para la exportación completa
            var logsToExport = _weightLogs.OrderByDescending(w => w.Date).ThenByDescending(w => w.Time).ToList();

            // Generar CSV
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true
            });

            // Mapear WeightLogDto a CSV record
            var records = logsToExport.Select(log => new WeightLogCsvRecord
            {
                Date = log.Date.ToString("dd/MM/yyyy"),
                Time = log.Time.ToString("HH:mm"),
                Weight = $"{ConvertedWeight(log.Weight):F1}",
                Unit = WeightUnit,
                Trend = GetTrendText(log.Trend),
                Note = log.Note ?? string.Empty
            });

            csv.WriteRecords(records);
            await writer.FlushAsync();

            var csvData = Convert.ToBase64String(memoryStream.ToArray());
            var fileName = $"peso_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            // Descargar archivo via JS Interop
            await JSRuntime.InvokeVoidAsync(
                "downloadFileFromBase64",
                fileName,
                "text/csv",
                csvData);

            Logger.LogInformation(
                "Dashboard: CSV export completed - {Count} records exported to {FileName}",
                logsToExport.Count,
                fileName);

            Snackbar.Add(ExportSuccess(logsToExport.Count), Severity.Success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Dashboard: Error exporting weight logs to CSV for user {UserId}", _currentUserId);
            Snackbar.Add(ErrorExportingData, Severity.Error);
        }
        finally
        {
            _isExporting = false;
        }
    }

    /// <summary>
    /// Obtiene el texto de tendencia localizado
    /// </summary>
    private string GetTrendText(WeightTrend trend)
    {
        return trend switch
        {
            WeightTrend.Up => "↑",
            WeightTrend.Down => "↓",
            WeightTrend.Neutral => "=",
            _ => "-"
        };
    }

    private async Task OpenAddWeightDialog()
    {
        Logger.LogInformation("Dashboard: Opening AddWeightDialog for user {UserId}", _currentUserId);

        var dialog = await DialogService.ShowAsync<AddWeightDialog>(AddWeightDialogTitle);
        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            Logger.LogInformation("Dashboard: Weight added successfully, reloading data");
            // ✅ NO mostrar notificación aquí - AddWeightDialog ya la mostró
            // Evita duplicación en historial de notificaciones
            await LoadDataAsync();
            StateHasChanged();
        }
        else
        {
            Logger.LogDebug("Dashboard: AddWeightDialog canceled");
        }
    }

    // ========================================================================
    // UNIT CONVERSION HELPERS (uses global UserStateService)
    // ========================================================================

    /// <summary>
    /// Formats weight in user's preferred unit (kg or lb) with unit label.
    /// Example: "75.5 kg" or "166.4 lb"
    /// </summary>
    private string FormatWeight(decimal weightInKg, int decimals = 1)
    {
        var converted = UserStateService.ConvertWeight(weightInKg);
        var unit = UserStateService.GetWeightUnitLabel();
        var format = $"F{decimals}";
        return $"{converted.ToString(format)} {unit}";
    }

    /// <summary>
    /// Converts weight to user's preferred unit WITHOUT label.
    /// Use for chart data points or calculations.
    /// </summary>
    private decimal ConvertWeight(decimal weightInKg)
    {
        return UserStateService.ConvertWeight(weightInKg);
    }

    /// <summary>
    /// Gets the weight unit label (kg or lb) based on user's preference.
    /// </summary>
    private string GetWeightUnitLabel()
    {
        return UserStateService.GetWeightUnitLabel();
    }

    // CSV Record class for export
    private sealed class WeightLogCsvRecord
    {
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string Trend { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }
}
