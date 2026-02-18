using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ControlPeso.Web.Components.Shared;

public partial class TrendCard
{
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;
    [Parameter, EditorRequired] public decimal CurrentValue { get; set; }
    [Parameter] public decimal? PreviousValue { get; set; }
    [Parameter] public string Unit { get; set; } = "kg";
    [Parameter] public string Icon { get; set; } = Icons.Material.Filled.TrendingUp;
    [Parameter] public Color IconColor { get; set; } = Color.Primary;
    [Parameter] public bool ShowChange { get; set; } = true;
    [Parameter] public int DecimalPlaces { get; set; } = 1;
    
    /// <summary>
    /// Para peso: true = menor es mejor (verde cuando baja)
    /// Para otras métricas: false = mayor es mejor (verde cuando sube)
    /// </summary>
    [Parameter] public bool LowerIsBetter { get; set; } = true;

    private decimal? PercentageChange
    {
        get
        {
            if (PreviousValue is null || PreviousValue == 0) return null;
            return ((CurrentValue - PreviousValue.Value) / PreviousValue.Value) * 100;
        }
    }

    private string FormattedValue => $"{CurrentValue.ToString($"F{DecimalPlaces}")} {Unit}";

    private Color GetChangeColor()
    {
        if (PercentageChange is null) return Color.Default;
        
        var isIncrease = PercentageChange > 0;
        
        // Si LowerIsBetter (peso): verde cuando baja (negative %), rojo cuando sube (positive %)
        // Si !LowerIsBetter (otros): verde cuando sube (positive %), rojo cuando baja (negative %)
        if (LowerIsBetter)
        {
            return isIncrease ? Color.Warning : Color.Success;
        }
        else
        {
            return isIncrease ? Color.Success : Color.Warning;
        }
    }

    private string GetChangeIcon()
    {
        if (PercentageChange is null) return Icons.Material.Filled.TrendingFlat;
        return PercentageChange > 0 
            ? Icons.Material.Filled.TrendingUp 
            : Icons.Material.Filled.TrendingDown;
    }

    private string GetChangeText()
    {
        if (PercentageChange is null) return "Sin datos previos";
        
        var absChange = Math.Abs(PercentageChange.Value);
        var direction = PercentageChange > 0 ? "↑" : "↓";
        
        return $"{direction} {absChange:F1}%";
    }
}
