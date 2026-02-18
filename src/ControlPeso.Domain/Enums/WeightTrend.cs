namespace ControlPeso.Domain.Enums;

/// <summary>
/// Tendencia del peso respecto al registro anterior.
/// Mapea el campo INTEGER Trend en la tabla WeightLogs.
/// </summary>
public enum WeightTrend
{
    /// <summary>
    /// El peso subió respecto al registro anterior.
    /// </summary>
    Up = 0,

    /// <summary>
    /// El peso bajó respecto al registro anterior.
    /// </summary>
    Down = 1,

    /// <summary>
    /// El peso se mantuvo igual o es el primer registro.
    /// </summary>
    Neutral = 2
}
