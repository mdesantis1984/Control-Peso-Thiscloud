namespace ControlPeso.Web.Extensions;

/// <summary>
/// Extension methods for configuring security policies (HSTS, HTTPS redirection).
/// </summary>
public static class SecurityPolicyExtensions
{
    /// <summary>
    /// Configures HSTS (HTTP Strict Transport Security) for production.
    /// Enforces HTTPS for 1 year with includeSubDomains and preload directives.
    /// </summary>
    public static IServiceCollection AddProductionHsts(this IServiceCollection services, IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            services.AddHsts(options =>
            {
                // HSTS max-age: 1 year (recommended for production)
                options.MaxAge = TimeSpan.FromDays(365);

                // Include subdomains (e.g., *.controlpeso.thiscloud.com.ar)
                options.IncludeSubDomains = true;

                // Preload: Allow browser vendors to hardcode HSTS
                // WARNING: Submit to hstspreload.org only if domain will ALWAYS use HTTPS
                options.Preload = false; // Set to true after verifying HTTPS stability
            });
        }

        return services;
    }

    /// <summary>
    /// Configures HTTPS redirection with status code 308 (Permanent Redirect) for production.
    /// </summary>
    public static IServiceCollection AddProductionHttpsRedirection(this IServiceCollection services, IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            services.AddHttpsRedirection(options =>
            {
                // 308 Permanent Redirect (preserves HTTP method, better than 301)
                options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;

                // HTTPS port (443 default, NPM Plus handles this)
                options.HttpsPort = 443;
            });
        }

        return services;
    }
}
