using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace ControlPeso.Application.Tests.Validators.Integration;

/// <summary>
/// Clase base para tests de integración de localización de validators.
/// Configura IStringLocalizer real con recursos .resx para verificar traducciones.
/// </summary>
public abstract class ValidatorLocalizationTestsBase
{
    /// <summary>
    /// Crea un IStringLocalizer configurado con cultura específica.
    /// Usa la infraestructura real de Microsoft.Extensions.Localization.
    /// Los archivos .resx están en ControlPeso.Shared.Resources/Validators/.
    /// </summary>
    /// <typeparam name="T">Tipo del validator para el cual crear el localizer</typeparam>
    /// <param name="culture">Cultura (ej: "es-AR", "en-US")</param>
    /// <returns>IStringLocalizer funcional con recursos .resx cargados</returns>
    protected IStringLocalizer<T> CreateLocalizer<T>(string culture)
    {
        // Configurar CultureInfo
        var cultureInfo = new CultureInfo(culture);
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        // Configurar ServiceCollection con localization services
        var services = new ServiceCollection();

        // Agregar logging (requerido por IStringLocalizer)
        services.AddLogging(builder => builder.AddDebug());

        // Agregar localization con ruta de recursos correcta
        // Los .resx están en ControlPeso.Shared.Resources assembly
        services.AddLocalization();

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // Obtener factory del service provider
        var factory = serviceProvider.GetRequiredService<IStringLocalizerFactory>();

        // Crear localizer tipado para T
        // Los recursos están en el namespace: ControlPeso.Shared.Resources.Validators.{TypeName}
        // El segundo parámetro es el location (base name) que debe coincidir con el namespace del .resx
        var baseName = $"ControlPeso.Shared.Resources.Validators.{typeof(T).Name}";
        var location = "ControlPeso.Shared.Resources";
        var localizer = factory.Create(baseName, location);

        // Crear un wrapper simple para devolver IStringLocalizer<T>
        return new TypedStringLocalizerWrapper<T>(localizer);
    }

    /// <summary>
    /// Verifica que una clave de traducción existe y devuelve un valor no vacío.
    /// </summary>
    /// <param name="localizer">IStringLocalizer a verificar</param>
    /// <param name="key">Clave de traducción</param>
    /// <param name="culture">Cultura esperada (para mensaje de error)</param>
    protected void AssertTranslationExists(IStringLocalizer localizer, string key, string culture)
    {
        var localizedString = localizer[key];

        // Verificar que no devuelve la clave misma (ResourceNotFound)
        Assert.NotEqual(key, localizedString.Value);

        // Verificar que no está vacío
        Assert.False(string.IsNullOrWhiteSpace(localizedString.Value),
            $"Translation key '{key}' for culture '{culture}' returned empty value");
    }

    /// <summary>
    /// Verifica que una clave con parámetros existe y formatea correctamente.
    /// </summary>
    /// <param name="localizer">IStringLocalizer a verificar</param>
    /// <param name="key">Clave de traducción</param>
    /// <param name="culture">Cultura esperada</param>
    /// <param name="args">Argumentos de formato</param>
    protected void AssertTranslationExistsWithArgs(IStringLocalizer localizer, string key, string culture, params object[] args)
    {
        var localizedString = localizer[key, args];

        // Verificar que no devuelve la clave misma
        Assert.NotEqual(key, localizedString.Value);

        // Verificar que no está vacío
        Assert.False(string.IsNullOrWhiteSpace(localizedString.Value),
            $"Translation key '{key}' for culture '{culture}' returned empty value");

        // Verificar que contiene al menos uno de los argumentos (fue formateado)
        var containsArg = args.Any(arg => localizedString.Value.Contains(arg.ToString()!));
        Assert.True(containsArg,
            $"Translation key '{key}' for culture '{culture}' did not format arguments correctly. Value: '{localizedString.Value}'");
    }
}

/// <summary>
/// Wrapper simple para convertir IStringLocalizer no tipado a IStringLocalizer&lt;T&gt; tipado.
/// Necesario porque ResourceManagerStringLocalizer no implementa IStringLocalizer&lt;T&gt; directamente.
/// </summary>
internal sealed class TypedStringLocalizerWrapper<T> : IStringLocalizer<T>
{
    private readonly IStringLocalizer _inner;

    public TypedStringLocalizerWrapper(IStringLocalizer inner)
    {
        _inner = inner;
    }

    public LocalizedString this[string name] => _inner[name];

    public LocalizedString this[string name, params object[] arguments] => _inner[name, arguments];

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        _inner.GetAllStrings(includeParentCultures);
}
