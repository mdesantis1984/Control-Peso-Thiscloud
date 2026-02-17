namespace ControlPeso.Domain.Enums;

/// <summary>
/// Unidad de peso para display.
/// Mapea el campo INTEGER DisplayUnit en la tabla WeightLogs.
/// Nota: El peso siempre se almacena en kg internamente.
/// </summary>
public enum WeightUnit
{
    /// <summary>
    /// Kilogramos.
    /// </summary>
    Kg = 0,

    /// <summary>
    /// Libras.
    /// </summary>
    Lb = 1
}
