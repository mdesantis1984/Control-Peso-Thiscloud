using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace ControlPeso.Web.Extensions;

/// <summary>
/// Extension methods for configuring forwarded headers in production behind a reverse proxy (NPM Plus).
/// </summary>
public static class ForwardedHeadersExtensions
{
    /// <summary>Configuration section holding the trusted proxy lists.</summary>
    public const string ConfigSection = "ForwardedHeaders";

    /// <summary>Docker private ranges and the LAN where NPMplus runs; used when nothing is configured.</summary>
    private static readonly string[] DefaultKnownNetworks = ["172.16.0.0/12", "192.168.178.0/24"];

    /// <summary>
    /// Configures forwarded headers for production deployment behind the NPMplus reverse proxy.
    /// Required for: correct HTTPS redirection, OAuth redirect URIs, IP logging, HSTS.
    /// Only proxies listed in <c>ForwardedHeaders:KnownNetworks</c> (CIDR) and <c>ForwardedHeaders:KnownProxies</c> (IP)
    /// are trusted; each list accepts an array or a comma-separated value. When both are absent or empty the
    /// defaults apply (172.16.0.0/12 and 192.168.178.0/24). The lists are never both emptied (that would trust every client).
    /// </summary>
    /// <exception cref="InvalidOperationException">A configured CIDR or IP address cannot be parsed.</exception>
    public static IServiceCollection AddForwardedHeadersConfiguration(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(configuration);

        if (!environment.IsDevelopment())
        {
            var networks = ReadList(configuration, $"{ConfigSection}:KnownNetworks")
                .Select(value => ParseNetwork($"{ConfigSection}:KnownNetworks", value))
                .ToList();
            var proxies = ReadList(configuration, $"{ConfigSection}:KnownProxies")
                .Select(value => ParseProxy($"{ConfigSection}:KnownProxies", value))
                .ToList();

            if (networks.Count == 0 && proxies.Count == 0)
            {
                networks = DefaultKnownNetworks
                    .Select(value => ParseNetwork($"{ConfigSection}:KnownNetworks", value))
                    .ToList();
            }

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                // NPM Plus forwards these headers
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                          ForwardedHeaders.XForwardedProto |
                                          ForwardedHeaders.XForwardedHost;

                // Replace the framework defaults (loopback only) with the configured trusted proxies.
                // At least one entry is always present, so the middleware never trusts every client.
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
                foreach (var network in networks)
                {
                    options.KnownIPNetworks.Add(network);
                }

                foreach (var proxy in proxies)
                {
                    options.KnownProxies.Add(proxy);
                }

                // Required for OAuth callbacks (https://controlpeso.thiscloud.com.ar/signin-google)
                options.ForwardedHostHeaderName = "X-Forwarded-Host";
                options.ForwardedProtoHeaderName = "X-Forwarded-Proto";

                // Limit forwarded header count (security)
                options.ForwardLimit = 1;

                // Required for correct scheme detection (http vs https)
                options.RequireHeaderSymmetry = false;
            });
        }

        return services;
    }

    private static List<string> ReadList(IConfiguration configuration, string key)
    {
        var section = configuration.GetSection(key);
        var values = new List<string>();

        if (!string.IsNullOrWhiteSpace(section.Value))
        {
            values.AddRange(section.Value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        foreach (var child in section.GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(child.Value))
            {
                values.AddRange(child.Value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
        }

        return values;
    }

    private static System.Net.IPNetwork ParseNetwork(string key, string value) =>
        System.Net.IPNetwork.TryParse(value, out var network)
            ? network
            : throw new InvalidOperationException($"Invalid CIDR '{value}' in configuration key {key}.");

    private static IPAddress ParseProxy(string key, string value) =>
        IPAddress.TryParse(value, out var address)
            ? address
            : throw new InvalidOperationException($"Invalid IP address '{value}' in configuration key {key}.");

    /// <summary>
    /// Adds CORS policy for production domain if needed (currently not required).
    /// Blazor Server uses SignalR, not CORS for same-origin communication.
    /// </summary>
    public static IServiceCollection AddProductionCors(this IServiceCollection services, IWebHostEnvironment environment)
    {
        // CORS is NOT needed for Blazor Server (SignalR uses WebSockets, not CORS)
        // Only required if exposing REST APIs to external domains

        // Example CORS config (currently disabled):
        /*
        if (!environment.IsDevelopment())
        {
            services.AddCors(options =>
            {
                options.AddPolicy("ProductionPolicy", policy =>
                {
                    policy.WithOrigins("https://controlpeso.thiscloud.com.ar")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });
        }
        */

        return services;
    }
}
