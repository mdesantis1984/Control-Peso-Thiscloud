using Microsoft.AspNetCore.DataProtection;

namespace ControlPeso.Web.Extensions;

/// <summary>Persists the Data Protection key ring to a configured directory (a mounted volume in Docker).</summary>
public static class DataProtectionExtensions
{
    /// <summary>
    /// When <c>DataProtection:Path</c> is set, keys are stored in that directory so auth cookies survive restarts
    /// and redeployments. Unset: framework defaults, unchanged.
    /// <para>
    /// The application discriminator is deliberately NOT set: it is part of the purpose chain, so changing it
    /// would make every cookie issued by the current deployment unreadable (forced re-login).
    /// </para>
    /// </summary>
    public static IServiceCollection AddConfiguredDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var path = configuration["DataProtection:Path"];
        if (!string.IsNullOrWhiteSpace(path))
        {
            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(path));
        }

        return services;
    }
}
