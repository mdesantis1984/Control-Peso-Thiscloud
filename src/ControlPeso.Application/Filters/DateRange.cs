namespace ControlPeso.Application.Filters;

/// <summary>
/// Rango de fechas para filtros y consultas.
/// </summary>
public sealed record DateRange
{
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }

    /// <summary>
    /// Cantidad de días en el rango (inclusive).
    /// </summary>
    public int DaysInRange => EndDate.DayNumber - StartDate.DayNumber + 1;

    /// <summary>
    /// Valida que StartDate <= EndDate.
    /// </summary>
    public bool IsValid => StartDate <= EndDate;

    /// <summary>
    /// Crea un rango de los últimos N días desde hoy.
    /// </summary>
    public static DateRange LastDays(int days)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new DateRange
        {
            StartDate = today.AddDays(-days),
            EndDate = today
        };
    }

    /// <summary>
    /// Crea un rango para el mes actual.
    /// </summary>
    public static DateRange CurrentMonth()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var firstDay = new DateOnly(today.Year, today.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        return new DateRange
        {
            StartDate = firstDay,
            EndDate = lastDay
        };
    }
}
