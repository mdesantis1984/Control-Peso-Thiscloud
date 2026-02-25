using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace ControlPeso.Shared.Resources.Localization;

/// <summary>
/// Custom IStringLocalizerFactory that resolves localized resources from the Shared.Resources assembly.
/// This factory maps types from Web/Application assemblies to their corresponding .resx files in Shared.Resources,
/// maintaining cohesion (IStringLocalizer&lt;Home&gt; → Home.resx) while centralizing resources for testability and reusability.
/// </summary>
/// <remarks>
/// Architecture: This is a cross-cutting concern that bridges Web/Application layers (requesting localization)
/// with Shared.Resources (providing localization). It respects Onion Architecture by not creating dependencies
/// from domain/application TO infrastructure, but instead infrastructure (this factory) knowing about both.
/// 
/// Scalability: New modules/domains can be added by extending the namespace mapping logic in GetResourcePath().
/// </remarks>
public sealed class SharedResourceStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly LocalizationOptions _localizationOptions;
    private readonly ILogger<SharedResourceStringLocalizerFactory> _logger;

    // Cache to avoid repeated reflection/resource lookup
    private readonly Dictionary<Type, IStringLocalizer> _localizerCache = new();
    private readonly object _cacheLock = new();

    public SharedResourceStringLocalizerFactory(
        IOptions<LocalizationOptions> localizationOptions,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(localizationOptions);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _localizationOptions = localizationOptions.Value;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<SharedResourceStringLocalizerFactory>();
    }

    /// <summary>
    /// Creates an IStringLocalizer for the specified type.
    /// Maps the type's namespace to the corresponding resource path in Shared.Resources assembly.
    /// </summary>
    public IStringLocalizer Create(Type resourceSource)
    {
        ArgumentNullException.ThrowIfNull(resourceSource);

        // Check cache first (thread-safe)
        lock (_cacheLock)
        {
            if (_localizerCache.TryGetValue(resourceSource, out var cachedLocalizer))
            {
                return cachedLocalizer;
            }
        }

        // Create new localizer
        var localizer = CreateLocalizerInternal(resourceSource);

        // Cache it
        lock (_cacheLock)
        {
            _localizerCache[resourceSource] = localizer;
        }

        return localizer;
    }

    /// <summary>
    /// Creates an IStringLocalizer for a specific base name and location.
    /// Used for non-typed localization scenarios.
    /// </summary>
    public IStringLocalizer Create(string baseName, string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        _logger.LogDebug(
            "Creating string localizer - BaseName: {BaseName}, Location: {Location}",
            baseName, location);

        // baseName is the resource name (e.g., "Home", "NavMenu")
        // location is the namespace (e.g., "ControlPeso.Web.Components.Pages")
        var resourcePath = GetResourcePathFromNamespace(location, baseName);

        return new SharedResourceStringLocalizer(
            baseName,
            resourcePath,
            _loggerFactory.CreateLogger<SharedResourceStringLocalizer>());
    }

    private IStringLocalizer CreateLocalizerInternal(Type resourceSource)
    {
        var typeName = resourceSource.Name;
        var typeNamespace = resourceSource.Namespace ?? string.Empty;

        _logger.LogDebug(
            "Creating string localizer for type - Type: {TypeName}, Namespace: {TypeNamespace}",
            typeName, typeNamespace);

        var resourcePath = GetResourcePathFromNamespace(typeNamespace, typeName);

        // Create typed localizer wrapper
        var localizerType = typeof(SharedResourceStringLocalizer<>).MakeGenericType(resourceSource);
        var localizer = (IStringLocalizer)Activator.CreateInstance(
            localizerType,
            typeName,
            resourcePath,
            _loggerFactory.CreateLogger(localizerType))!;

        return localizer;
    }

    /// <summary>
    /// Maps a type namespace to the corresponding resource path in Shared.Resources assembly.
    /// This is where the namespace-to-path mapping logic lives - extend here for new modules/domains.
    /// </summary>
    /// <example>
    /// ControlPeso.Web.Components.Pages.Home → Components/Pages/Home
    /// ControlPeso.Web.Components.Shared.NavMenu → Components/Shared/NavMenu
    /// ControlPeso.Web.Components.Layout.MainLayout → Components/Layout/MainLayout
    /// ControlPeso.Application.Validators.CreateWeightLogValidator → Validators/CreateWeightLogValidator
    /// ControlPeso.Web.Pages.Dashboard → Pages/Dashboard
    /// </example>
    private string GetResourcePathFromNamespace(string typeNamespace, string typeName)
    {
        // Handle validators (Application layer)
        if (typeNamespace.StartsWith("ControlPeso.Application.Validators", StringComparison.Ordinal))
        {
            return $"Validators/{typeName}";
        }

        // Handle Web components - ALL go under Components/ prefix
        if (typeNamespace.StartsWith("ControlPeso.Web.Components.", StringComparison.Ordinal))
        {
            // ControlPeso.Web.Components.Pages.Home → Components/Pages/Home
            // ControlPeso.Web.Components.Shared.NavMenu → Components/Shared/NavMenu
            // ControlPeso.Web.Components.Layout.MainLayout → Components/Layout/MainLayout
            var afterComponents = typeNamespace.Substring("ControlPeso.Web.Components.".Length);

            // All components map to Components/{subpath}/{typeName}
            return $"Components/{afterComponents}/{typeName}";
        }

        // Handle Web pages (top-level Pages, not Components.Pages)
        if (typeNamespace.StartsWith("ControlPeso.Web.Pages", StringComparison.Ordinal))
        {
            // ControlPeso.Web.Pages.Dashboard → Pages/Dashboard
            return $"Pages/{typeName}";
        }

        // Fallback: assume it's in root of Shared.Resources
        _logger.LogWarning(
            "No specific mapping found for namespace {Namespace}, using root path for {TypeName}",
            typeNamespace, typeName);

        return typeName;
    }
}
