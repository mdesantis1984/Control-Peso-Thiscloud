using System.Reflection;

namespace ControlPeso.Web.Components.Shared;

/// <summary>
/// Single source of truth for the application version shown in the UI:
/// the assembly informational version (build metadata after '+' is dropped).
/// </summary>
public static class AppVersionInfo
{
    private static readonly Lazy<string> CurrentVersion = new(() => Resolve(typeof(AppVersionInfo).Assembly));

    /// <summary>Version of the running application, for example "1.0.0".</summary>
    public static string Current => CurrentVersion.Value;

    /// <summary>
    /// Resolves the display version of an assembly from its informational version,
    /// falling back to the assembly version when the attribute is missing or empty.
    /// </summary>
    public static string Resolve(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            var plus = informational.IndexOf('+');
            var version = plus >= 0 ? informational[..plus] : informational;
            if (!string.IsNullOrWhiteSpace(version))
            {
                return version.Trim();
            }
        }

        var fallback = assembly.GetName().Version;
        return fallback is null ? "0.0.0" : $"{fallback.Major}.{fallback.Minor}.{fallback.Build}";
    }
}
