using System.Text.Json;
using ControlPeso.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ControlPeso.Web.Extensions;

/// <summary>
/// Liveness (<c>/health</c>, no dependencies) and readiness (<c>/health/ready</c>, checks the database) endpoints.
/// Both are anonymous and excluded from rate limiting so orchestrators and uptime probes are never throttled.
/// </summary>
public static class HealthEndpointsExtensions
{
    /// <summary>Tag of the checks that decide readiness.</summary>
    public const string ReadyTag = "ready";

    /// <summary>Registers the database readiness check (<see cref="ControlPesoDbContext"/>).</summary>
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks()
            .AddDbContextCheck<ControlPesoDbContext>(name: "database", tags: [ReadyTag]);

        return services;
    }

    /// <summary>Maps <c>/health</c> (liveness) and <c>/health/ready</c> (readiness, 503 when the database is unreachable).</summary>
    public static IEndpointRouteBuilder MapApplicationHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Liveness: the process answers; never touches the database
        endpoints.MapGet("/health", () => Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        }))
        .WithName("HealthCheck")
        .WithTags("Health")
        .AllowAnonymous()
        .DisableRateLimiting()
        .ExcludeFromDescription();

        // Readiness: Healthy -> 200, Unhealthy -> 503 (default status code mapping)
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadyTag),
            ResponseWriter = WriteReadinessAsync
        })
        .WithTags("Health")
        .AllowAnonymous()
        .DisableRateLimiting();

        return endpoints;
    }

    /// <summary>Writes only check names and statuses: failure details never reach anonymous callers.</summary>
    private static Task WriteReadinessAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new { name = entry.Key, status = entry.Value.Status.ToString() })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
