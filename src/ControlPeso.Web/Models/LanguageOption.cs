namespace ControlPeso.Web.Models;

/// <summary>
/// Representa una opción de idioma con su código ISO, etiqueta de país y bandera.
/// </summary>
public sealed class LanguageOption
{
    /// <summary>
    /// Código ISO 639-1 del idioma (en, es, it, fr).
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Etiqueta del país para mostrar en UI (USA, ARG, ITA, FRA).
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Código ISO 3166-1 alpha-2 del país para Flagpedia API (us, ar, it, fr).
    /// </summary>
    public required string CountryCode { get; init; }
}
