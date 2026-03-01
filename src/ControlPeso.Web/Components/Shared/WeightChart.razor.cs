using ControlPeso.Application.DTOs;
using ControlPeso.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace ControlPeso.Web.Components.Shared;

/// <summary>
/// WeightChart - Gráfico de evolución de peso con MudChart
/// Muestra la tendencia del peso a lo largo del tiempo
/// </summary>
public partial class WeightChart : IDisposable
{
    [Inject] private ILogger<WeightChart> Logger { get; set; } = null!;
    [Inject] private IStringLocalizer<WeightChart> Localizer { get; set; } = null!;
    [Inject] private UserStateService UserStateService { get; set; } = null!;

    /// <summary>
    /// Datos de registros de peso a graficar
    /// </summary>
    [Parameter, EditorRequired]
    public List<WeightLogDto> Data { get; set; } = new();

    // Localized Properties
    private string NoDataAvailable => Localizer[nameof(NoDataAvailable)];

    /// <summary>
    /// Título del gráfico
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "Evolución del Peso";

    /// <summary>
    /// Altura del gráfico en píxeles
    /// </summary>
    [Parameter]
    public int Height { get; set; } = 350;

    private bool _isLoading = true;
    private bool _isDisposed = false; // Protección contra render después de dispose
    private List<ChartSeries<double>> _series = new();
    private string[] _labels = Array.Empty<string>();
    private LineChartOptions _chartOptions = new();

    // Minimum data points required for NaturalSpline interpolation
    private const int MinPointsForSpline = 4;

    protected override void OnInitialized()
    {
        // Configure chart options - interpolation will be set dynamically based on data count
        _chartOptions.YAxisTicks = 5;
        _chartOptions.YAxisLines = true;
        _chartOptions.XAxisLines = false;

        // Subscribe to unit system changes to refresh chart
        UserStateService.UserUnitSystemUpdated += OnUnitSystemChanged;
    }

    protected override void OnParametersSet()
    {
        // Skip if already disposed (prevents "renderer does not have component" error)
        if (_isDisposed) return;

        _isLoading = true;

        try
        {
            if (Data == null || Data.Count == 0)
            {
                Logger.LogDebug("WeightChart: No data provided");
                _series.Clear();
                _labels = Array.Empty<string>();
                return;
            }

            Logger.LogInformation("WeightChart: Rendering chart with {Count} data points", Data.Count);

            // Ordenar datos por fecha (más antiguos primero)
            var orderedData = Data.OrderBy(d => d.Date).ToList();

            // CRITICAL: NaturalSpline requires at least 4 data points
            // Use Straight (linear) interpolation for fewer points to avoid crash
            _chartOptions.InterpolationOption = orderedData.Count >= MinPointsForSpline 
                ? InterpolationOption.NaturalSpline 
                : InterpolationOption.Straight;

            Logger.LogDebug("WeightChart: Using interpolation {Interpolation} for {Count} points",
                _chartOptions.InterpolationOption, orderedData.Count);

            // Crear etiquetas del eje X (fechas)
            _labels = orderedData.Select(d => d.Date.ToString("dd/MM")).ToArray();

            // Get current unit label
            var unitLabel = UserStateService.GetWeightUnitLabel();

            // Crear serie de datos (converting to user's preferred unit)
            _series = new List<ChartSeries<double>>
            {
                new ChartSeries<double>
                {
                    Name = $"Peso ({unitLabel})",
                    Data = orderedData.Select(d => (double)UserStateService.ConvertWeight(d.Weight)).ToArray()
                }
            };

            Logger.LogDebug(
                "WeightChart: Chart configured - Labels: {LabelCount}, Series: {SeriesCount}, Unit: {Unit}",
                _labels.Length, _series.Count, unitLabel);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "WeightChart: Error rendering chart");
            _series.Clear();
            _labels = Array.Empty<string>();
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Handler for UnitSystem changes - refreshes chart when user changes preferences.
    /// Protected against disposed component state.
    /// </summary>
    private async void OnUnitSystemChanged(object? sender, Domain.Enums.UnitSystem newUnitSystem)
    {
        // Skip if disposed (prevents "renderer does not have component" error)
        if (_isDisposed) return;

        try
        {
            Logger.LogDebug("WeightChart: Unit system changed to {UnitSystem} - refreshing chart", newUnitSystem);

            // Re-process data with new unit system (this is safe - just updates local state)
            if (Data != null && Data.Count > 0)
            {
                var orderedData = Data.OrderBy(d => d.Date).ToList();

                // CRITICAL: Update interpolation based on data count
                _chartOptions.InterpolationOption = orderedData.Count >= MinPointsForSpline 
                    ? InterpolationOption.NaturalSpline 
                    : InterpolationOption.Straight;

                _labels = orderedData.Select(d => d.Date.ToString("dd/MM")).ToArray();
                var unitLabel = UserStateService.GetWeightUnitLabel();
                _series = new List<ChartSeries<double>>
                {
                    new ChartSeries<double>
                    {
                        Name = $"Peso ({unitLabel})",
                        Data = orderedData.Select(d => (double)UserStateService.ConvertWeight(d.Weight)).ToArray()
                    }
                };
            }

            // Check again before invoking StateHasChanged (component may have been disposed while processing)
            if (!_isDisposed)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (ObjectDisposedException)
        {
            // Component was disposed during async operation - expected, ignore
            Logger.LogDebug("WeightChart: Component disposed during unit system change - ignoring");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "WeightChart: Error handling unit system change");
        }
    }

    public void Dispose()
    {
        // Mark as disposed FIRST to prevent any pending operations
        _isDisposed = true;

        // Unsubscribe from events to prevent memory leaks
        UserStateService.UserUnitSystemUpdated -= OnUnitSystemChanged;
    }
}
