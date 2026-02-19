using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ControlPeso.Web.Components.Shared;

/// <summary>
/// StatsCard - Tarjeta reutilizable para mostrar métricas/estadísticas
/// Muestra un valor con título, ícono y tendencia opcional
/// </summary>
public partial class StatsCard
{
    /// <summary>
    /// Título de la métrica (ej: "Peso Actual", "Cambio Semanal")
    /// </summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Valor numérico de la métrica
    /// </summary>
    [Parameter, EditorRequired]
    public decimal Value { get; set; }

    /// <summary>
    /// Unidad de medida (ej: "kg", "lb", "%")
    /// </summary>
    [Parameter]
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Ícono de Material Design a mostrar
    /// </summary>
    [Parameter]
    public string Icon { get; set; } = Icons.Material.Filled.Info;

    /// <summary>
    /// Color del ícono
    /// </summary>
    [Parameter]
    public Color Color { get; set; } = Color.Primary;

    /// <summary>
    /// Si se debe mostrar el chip de tendencia
    /// </summary>
    [Parameter]
    public bool ShowTrend { get; set; } = false;

    /// <summary>
    /// Valor de la tendencia (positivo = subió, negativo = bajó)
    /// </summary>
    [Parameter]
    public decimal TrendValue { get; set; } = 0;

    /// <summary>
    /// Cantidad de decimales a mostrar en el valor
    /// </summary>
    [Parameter]
    public int DecimalPlaces { get; set; } = 1;

    private string FormattedValue => $"{Value.ToString($"F{DecimalPlaces}")} {Unit}";

    private Color GetTrendColor()
    {
        // Para peso: negativo (bajó) = verde (éxito)
        // Para peso: positivo (subió) = naranja (warning)
        return TrendValue < 0 ? Color.Success : Color.Warning;
    }

    private string GetTrendIcon()
    {
        return TrendValue < 0 
            ? Icons.Material.Filled.ArrowDownward 
            : Icons.Material.Filled.ArrowUpward;
    }
}
