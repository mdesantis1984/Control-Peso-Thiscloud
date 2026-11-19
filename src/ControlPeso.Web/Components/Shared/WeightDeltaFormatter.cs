using System.Globalization;

namespace ControlPeso.Web.Components.Shared;

/// <summary>
/// Formats signed weight deltas using the current (or an explicit) culture so the decimal separator
/// matches the weights shown next to them.
/// </summary>
public static class WeightDeltaFormatter
{
    /// <summary>
    /// Formats a delta with one decimal and an explicit sign: "+0,4", "-0,2" or "0,0".
    /// </summary>
    public static string Format(decimal delta, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        var rounded = Math.Round(delta, 1, MidpointRounding.AwayFromZero);
        if (rounded == 0m)
        {
            return 0m.ToString("F1", culture);
        }

        var sign = rounded > 0 ? "+" : "-";
        return sign + Math.Abs(rounded).ToString("F1", culture);
    }
}
