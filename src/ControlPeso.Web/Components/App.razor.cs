using Microsoft.AspNetCore.Components;

namespace ControlPeso.Web.Components;

/// <summary>
/// Code-behind para el componente raíz App.razor.
/// Obtiene configuración de Google Analytics desde appsettings.json.
/// </summary>
public partial class App : ComponentBase
{
    [Inject]
    private IConfiguration Configuration { get; set; } = default!;

    private string googleAnalyticsMeasurementId = string.Empty;

    /// <summary>
    /// Inicializa el componente y obtiene el Measurement ID de Google Analytics.
    /// </summary>
    protected override void OnInitialized()
    {
        // Obtener Measurement ID desde appsettings.json
        googleAnalyticsMeasurementId = Configuration["GoogleAnalytics:MeasurementId"] ?? string.Empty;
    }
}
