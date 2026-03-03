using ControlPeso.Web.Services;

namespace ControlPeso.Web.Extensions;

/// <summary>
/// Extension methods for registering SEO endpoints (robots.txt, sitemap.xml)
/// Uses Minimal APIs to ensure they are registered BEFORE Blazor routing
/// </summary>
public static class SeoEndpointsExtensions
{
    /// <summary>
    /// Maps SEO endpoints for robots.txt and sitemap.xml
    /// MUST be called BEFORE MapRazorComponents() to take precedence over Blazor routing
    /// </summary>
    public static IEndpointRouteBuilder MapSeoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // ============================================
        // GET /robots.txt - Dynamic robots.txt with 50+ AI crawlers
        // ============================================
        endpoints.MapGet("/robots.txt", async (
            SitemapService sitemapService,
            HttpContext context,
            ILogger<SitemapService> loggerParam) =>
        {
            loggerParam.LogInformation("🤖 /robots.txt requested from {UserAgent}", 
                context.Request.Headers.UserAgent.ToString());

            try
            {
                var robotsTxt = sitemapService.GenerateRobotsTxt();
                loggerParam.LogInformation("✅ Robots.txt generated - Length: {Length} bytes, AI Crawlers: 50+", 
                    robotsTxt.Length);

                // Set response headers
                context.Response.ContentType = "text/plain; charset=utf-8";
                context.Response.Headers["Cache-Control"] = "public, max-age=86400"; // 24 hours

                // Write directly to response
                await context.Response.WriteAsync(robotsTxt, context.RequestAborted);
            }
            catch (Exception ex)
            {
                loggerParam.LogError(ex, "❌ Error generating robots.txt");
                context.Response.StatusCode = 500;
            }
        })
        .WithName("GetRobotsTxt")
        .WithTags("SEO")
        .Produces<string>(200, "text/plain")
        .Produces(500)
        .ExcludeFromDescription(); // Don't show in Swagger (if added later)

        // ============================================
        // GET /sitemap.xml - Dynamic XML sitemap
        // ============================================
        endpoints.MapGet("/sitemap.xml", async (
            SitemapService sitemapService,
            HttpContext context,
            ILogger<SitemapService> loggerParam) =>
        {
            loggerParam.LogInformation("🗺️ /sitemap.xml requested from {UserAgent}", 
                context.Request.Headers.UserAgent.ToString());

            try
            {
                var sitemap = sitemapService.GenerateSitemap();
                loggerParam.LogInformation("✅ Sitemap generated - Length: {Length} bytes, URLs: {UrlCount}", 
                    sitemap.Length, 8); // 8 public URLs

                // Set response headers
                context.Response.ContentType = "application/xml; charset=utf-8";
                context.Response.Headers["Cache-Control"] = "public, max-age=3600"; // 1 hour

                // Write directly to response
                await context.Response.WriteAsync(sitemap, context.RequestAborted);
            }
            catch (Exception ex)
            {
                loggerParam.LogError(ex, "❌ Error generating sitemap.xml");
                context.Response.StatusCode = 500;
            }
        })
        .WithName("GetSitemap")
        .WithTags("SEO")
        .Produces<string>(200, "application/xml")
        .Produces(500)
        .ExcludeFromDescription(); // Don't show in Swagger (if added later)

        return endpoints;
    }
}
