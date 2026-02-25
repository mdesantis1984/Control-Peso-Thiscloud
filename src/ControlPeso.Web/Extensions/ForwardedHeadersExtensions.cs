using Microsoft.AspNetCore.HttpOverrides;

namespace ControlPeso.Web.Extensions;

/// <summary>
/// Extension methods for configuring forwarded headers in production behind a reverse proxy (NPM Plus).
/// </summary>
public static class ForwardedHeadersExtensions
{
    /// <summary>
    /// Configures forwarded headers for production deployment behind NPM Plus reverse proxy.
    /// Required for: correct HTTPS redirection, OAuth redirect URIs, IP logging, HSTS.
    /// </summary>
    public static IServiceCollection AddForwardedHeadersConfiguration(this IServiceCollection services, IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            // Configure forwarded headers for production (behind NPM Plus reverse proxy)
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                // NPM Plus forwards these headers
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                          ForwardedHeaders.XForwardedProto |
                                          ForwardedHeaders.XForwardedHost;

                // Trust all proxies (NPM Plus + Docker network)
                // Alternative: Add specific proxy IPs if known
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();

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
