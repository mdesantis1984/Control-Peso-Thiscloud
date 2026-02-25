namespace ControlPeso.Web.Helpers;

/// <summary>
/// Helper para obtener URLs de banderas desde Flagpedia CDN.
/// API estable y gratuita: https://flagcdn.com
/// </summary>
public static class Flagpedia
{
    /// <summary>
    /// Obtiene la URL de una bandera en formato w40 (40px de ancho).
    /// </summary>
    /// <param name="countryCode">Código ISO 3166-1 alpha-2 del país (us, ar, it, etc).</param>
    /// <returns>URL completa de la bandera en formato PNG.</returns>
    public static string GetFlag(string countryCode)
        => $"https://flagcdn.com/w40/{countryCode}.png";

    /// <summary>
    /// Obtiene la URL de una bandera con tamaño personalizado.
    /// </summary>
    /// <param name="countryCode">Código ISO 3166-1 alpha-2 del país.</param>
    /// <param name="width">Ancho deseado (20, 40, 80, 160, etc).</param>
    /// <returns>URL completa de la bandera en formato PNG.</returns>
    public static string GetFlag(string countryCode, int width)
        => $"https://flagcdn.com/w{width}/{countryCode}.png";
}
