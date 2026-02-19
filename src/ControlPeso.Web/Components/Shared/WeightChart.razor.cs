using ControlPeso.Application.DTOs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ControlPeso.Web.Components.Shared;

/// <summary>
/// WeightChart - Gráfico de evolución de peso con MudChart
/// Muestra la tendencia del peso a lo largo del tiempo
/// </summary>
public partial class WeightChart
{
    [Inject] private ILogger<WeightChart> Logger { get; set; } = null!;

    /// <summary>
    /// Datos de registros de peso a graficar
    /// </summary>
    [Parameter, EditorRequired]
    public List<WeightLogDto> Data { get; set; } = new();

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
    private List<ChartSeries> _series = new();
    private string[] _labels = Array.Empty<string>();
    private ChartOptions _options = new();

    protected override void OnParametersSet()
    {
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

            // Crear etiquetas del eje X (fechas)
            _labels = orderedData.Select(d => d.Date.ToString("dd/MM")).ToArray();

            // Crear serie de datos
            _series = new List<ChartSeries>
            {
                new ChartSeries
                {
                    Name = "Peso (kg)",
                    Data = orderedData.Select(d => (double)d.Weight).ToArray()
                }
            };

            // Configurar opciones del gráfico
            _options = new ChartOptions
            {
                // Interpolación suave para línea más fluida
                InterpolationOption = InterpolationOption.NaturalSpline,

                // Línea principal
                LineStrokeWidth = 3,

                // Ejes
                YAxisTicks = 10,
                YAxisLines = true,
                XAxisLines = false,
                YAxisFormat = "{0:F1} kg",

                // Diseño responsivo
                DisableLegend = false,

                // Paleta de colores (Primary theme color)
                ChartPalette = new[] { "#2196F3" }
            };

            Logger.LogDebug("WeightChart: Chart configured - Labels: {LabelCount}, Series: {SeriesCount}",
                _labels.Length, _series.Count);
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
}
