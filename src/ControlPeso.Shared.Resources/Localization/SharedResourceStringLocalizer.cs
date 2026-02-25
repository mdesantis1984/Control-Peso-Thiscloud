using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace ControlPeso.Shared.Resources.Localization;

/// <summary>
/// Custom IStringLocalizer implementation that loads resources from the Shared.Resources assembly.
/// This localizer resolves .resx files embedded in the Shared.Resources assembly using ResourceManager.
/// </summary>
public sealed class SharedResourceStringLocalizer : IStringLocalizer
{
    private readonly string _baseName;
    private readonly string _resourcePath;
    private readonly ILogger _logger;
    private readonly ResourceManager _resourceManager;
    private readonly Assembly _resourceAssembly;

    public SharedResourceStringLocalizer(
        string baseName,
        string resourcePath,
        ILogger logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourcePath);
        ArgumentNullException.ThrowIfNull(logger);

        _baseName = baseName;
        _resourcePath = resourcePath;
        _logger = logger;

        // Get the Shared.Resources assembly
        _resourceAssembly = typeof(SharedResourceStringLocalizer).Assembly;

        // Construct the full resource name: ControlPeso.Shared.Resources.{resourcePath}
        var fullResourceName = $"ControlPeso.Shared.Resources.{_resourcePath.Replace('/', '.')}";

        _logger.LogDebug(
            "Initializing SharedResourceStringLocalizer - BaseName: {BaseName}, ResourcePath: {ResourcePath}, FullResourceName: {FullResourceName}",
            _baseName, _resourcePath, fullResourceName);

        // Create ResourceManager pointing to the embedded resources in Shared.Resources assembly
        _resourceManager = new ResourceManager(fullResourceName, _resourceAssembly);
    }

    /// <summary>
    /// Gets the localized string for the specified key in the current culture.
    /// </summary>
    public LocalizedString this[string name]
    {
        get
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var value = GetStringInternal(name, CultureInfo.CurrentUICulture);
            return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
        }
    }

    /// <summary>
    /// Gets the localized string for the specified key with format arguments.
    /// </summary>
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var format = GetStringInternal(name, CultureInfo.CurrentUICulture);
            var value = format != null
                ? string.Format(CultureInfo.CurrentCulture, format, arguments)
                : name;

            return new LocalizedString(name, value, resourceNotFound: format == null);
        }
    }

    /// <summary>
    /// Gets all localized strings for the current culture, including parent cultures.
    /// </summary>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var culture = CultureInfo.CurrentUICulture;
        var resourceSet = _resourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: includeParentCultures);

        if (resourceSet == null)
        {
            _logger.LogWarning(
                "No resource set found for culture {Culture} in resource {ResourcePath}",
                culture.Name, _resourcePath);
            yield break;
        }

        foreach (System.Collections.DictionaryEntry entry in resourceSet)
        {
            var key = entry.Key.ToString()!;
            var value = entry.Value?.ToString();
            yield return new LocalizedString(key, value ?? key, resourceNotFound: value == null);
        }
    }

    private string? GetStringInternal(string name, CultureInfo culture)
    {
        try
        {
            var value = _resourceManager.GetString(name, culture);

            if (value == null)
            {
                _logger.LogWarning(
                    "Resource key not found - Key: {Key}, Culture: {Culture}, ResourcePath: {ResourcePath}, BaseName: {BaseName}",
                    name, culture.Name, _resourcePath, _baseName);
            }
            else
            {
                _logger.LogDebug(
                    "Resource key found - Key: {Key}, Culture: {Culture}, Value: {Value}",
                    name, culture.Name, value);
            }

            return value;
        }
        catch (MissingManifestResourceException ex)
        {
            _logger.LogError(ex,
                "Missing manifest resource - ResourcePath: {ResourcePath}, Culture: {Culture}, Assembly: {Assembly}",
                _resourcePath, culture.Name, _resourceAssembly.FullName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving localized string - Key: {Key}, Culture: {Culture}, ResourcePath: {ResourcePath}",
                name, culture.Name, _resourcePath);
            return null;
        }
    }
}

/// <summary>
/// Typed wrapper for SharedResourceStringLocalizer that implements IStringLocalizer&lt;T&gt;.
/// This maintains type safety while delegating to the non-generic implementation.
/// </summary>
public sealed class SharedResourceStringLocalizer<T> : IStringLocalizer<T>
{
    private readonly IStringLocalizer _localizer;

    public SharedResourceStringLocalizer(
        string baseName,
        string resourcePath,
        ILogger logger)
    {
        _localizer = new SharedResourceStringLocalizer(baseName, resourcePath, logger);
    }

    public LocalizedString this[string name] => _localizer[name];

    public LocalizedString this[string name, params object[] arguments] => _localizer[name, arguments];

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => _localizer.GetAllStrings(includeParentCultures);
}
