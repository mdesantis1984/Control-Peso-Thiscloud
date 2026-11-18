using System.Globalization;

namespace ControlPeso.Web.Pages.Legal;

/// <summary>
/// Formats legal document dates (long date pattern) for the language the document is displayed in.
/// The current culture is used when it matches the displayed language; otherwise a default
/// culture for that language (es-AR / en-US) is used.
/// </summary>
public static class LegalDateFormatter
{
    /// <summary>Date the Privacy Policy and Terms &amp; Conditions took effect and were last updated.</summary>
    public static readonly DateOnly DocumentDate = new(2026, 2, 25);

    public static string Format(DateOnly date, bool isEnglish)
    {
        var language = isEnglish ? "en" : "es";
        var current = CultureInfo.CurrentCulture;
        var culture = current.TwoLetterISOLanguageName == language
            ? current
            : CultureInfo.GetCultureInfo(isEnglish ? "en-US" : "es-AR");

        return date.ToDateTime(TimeOnly.MinValue).ToString("D", culture);
    }
}
