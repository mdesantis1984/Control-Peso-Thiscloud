namespace ControlPeso.Domain.Enums;

/// <summary>
/// Sistema de unidades preferido por el usuario para display.
/// Mapea el campo INTEGER UnitSystem en la tabla Users.
/// </summary>
public enum UnitSystem
{
    /// <summary>
    /// Sistema métrico (kg, cm, etc.).
    /// </summary>
    Metric = 0,

    /// <summary>
    /// Sistema imperial (lb, ft/in, etc.).
    /// </summary>
    Imperial = 1
}
