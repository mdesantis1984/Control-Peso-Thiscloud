using ControlPeso.Shared.Resources.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace ControlPeso.Shared.Resources.Extensions;

/// <summary>
/// Extension methods for registering Shared.Resources localization services.
/// This follows the same pattern as AddLocalization() but registers our custom factory.
/// </summary>
public static class LocalizationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SharedResourceStringLocalizerFactory as the IStringLocalizerFactory implementation.
    /// This replaces the default ResourceManagerStringLocalizerFactory to support cross-assembly resource loading.
    /// </summary>
    /// <remarks>
    /// Call this INSTEAD OF services.AddLocalization() to use Shared.Resources assembly for all localization.
    /// This maintains DI abstraction (components still inject IStringLocalizer&lt;T&gt;) while changing the implementation.
    /// </remarks>
    public static IServiceCollection AddSharedResourcesLocalization(
        this IServiceCollection services,
        Action<LocalizationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register LocalizationOptions (same as standard AddLocalization)
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            // Default configuration (no ResourcesPath needed since we use assembly-based loading)
            services.Configure<LocalizationOptions>(options =>
            {
                // ResourcesPath is ignored by our custom factory but set it for compatibility
                options.ResourcesPath = string.Empty;
            });
        }

        // Register our custom factory as singleton (same lifetime as default)
        services.AddSingleton<IStringLocalizerFactory, SharedResourceStringLocalizerFactory>();

        // Register generic IStringLocalizer<T> resolver
        // This is crucial - DI needs to know how to create IStringLocalizer<T> from IStringLocalizerFactory
        services.AddTransient(typeof(IStringLocalizer<>), typeof(FactoryStringLocalizer<>));

        return services;
    }
}
