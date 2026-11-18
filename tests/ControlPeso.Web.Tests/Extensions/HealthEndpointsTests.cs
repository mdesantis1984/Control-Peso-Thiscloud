using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using ControlPeso.Infrastructure.Extensions;
using ControlPeso.Web.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ControlPeso.Web.Tests.Extensions;

/// <summary>
/// S8: /health is liveness (no database), /health/ready checks the database and answers 503 when it is unreachable.
/// Both stay reachable with a deny-by-default auth policy and an aggressive global rate limiter.
/// </summary>
public sealed class HealthEndpointsTests : IAsyncLifetime
{
    private readonly string _scratch = Directory.CreateTempSubdirectory("controlpeso-health-").FullName;
    private readonly List<WebApplication> _apps = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var app in _apps)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(_scratch, recursive: true);
    }

    /// <summary>Starts the real health endpoints on loopback with SQLite; <paramref name="databasePath"/> decides reachability.</summary>
    private async Task<HttpClient> StartAsync(string databasePath, bool productionProxyPipeline = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = _scratch,
            EnvironmentName = productionProxyPipeline ? Environments.Production : Environments.Development,
            // No file watchers: CI and shared dev machines have a low inotify instance limit
            Args = ["--hostBuilder:reloadConfigOnChange=false"]
        });
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = $"Data Source={databasePath}"
        });
        builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);
        builder.Services.AddApplicationHealthChecks();

        // Hostile pipeline: everything requires authentication and only one request per minute is allowed
        builder.Services.AddAuthentication("none").AddCookie("none");
        builder.Services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        builder.Services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                RateLimitPartition.GetFixedWindowLimiter("all", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 1,
                    Window = TimeSpan.FromMinutes(1)
                }));
        });

        if (productionProxyPipeline)
        {
            // Same order as Program.cs: forwarded headers first, then HTTPS redirection
            builder.Configuration["HTTPS_PORT"] = "443";
            builder.Services.AddForwardedHeadersConfiguration(builder.Environment, builder.Configuration);
        }

        var app = builder.Build();
        if (productionProxyPipeline)
        {
            app.UseForwardedHeaders();
            app.UseHttpsRedirection();
        }

        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapApplicationHealthEndpoints();
        app.Urls.Add("http://127.0.0.1:0"); // ephemeral loopback port
        await app.StartAsync();
        _apps.Add(app);

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = new Uri(address) };
    }

    /// <summary>An existing SQLite file (EF's CanConnect is false for a missing file).</summary>
    private string ReachableDatabase()
    {
        var path = Path.Combine(_scratch, "health.db");
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE IF NOT EXISTS probe (id INTEGER)";
        command.ExecuteNonQuery();
        return path;
    }

    private string UnreachableDatabase() => Path.Combine(_scratch, "missing-directory", "health.db");

    [Fact]
    public async Task Ready_WithReachableDatabase_Returns200Healthy()
    {
        using var client = await StartAsync(ReachableDatabase());

        var response = await client.GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(body).RootElement.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task Ready_WithUnreachableDatabase_Returns503Unhealthy()
    {
        using var client = await StartAsync(UnreachableDatabase());

        var response = await client.GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        JsonDocument.Parse(body).RootElement.GetProperty("status").GetString().Should().Be("Unhealthy");
        body.Should().NotContain("missing-directory", "failure details must not leak to anonymous callers");
    }

    [Fact]
    public async Task Liveness_WithUnreachableDatabase_StillReturns200AndDoesNotTouchTheDatabase()
    {
        using var client = await StartAsync(UnreachableDatabase());

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(body).RootElement.GetProperty("status").GetString().Should().Be("healthy");
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoints_AreExcludedFromAuthenticationAndRateLimiting(string path)
    {
        using var client = await StartAsync(ReachableDatabase());

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
        {
            statuses.Add((await client.GetAsync(path)).StatusCode);
        }

        statuses.Should().OnlyContain(s => s == HttpStatusCode.OK);
    }

    [Fact]
    public async Task Liveness_FromLoopbackWithForwardedProtoHttps_Returns200WithoutHttpsRedirect()
    {
        // The container HEALTHCHECK runs `curl http://localhost:8080/health` with X-Forwarded-Proto: https
        using var client = await StartAsync(ReachableDatabase(), productionProxyPipeline: true);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Liveness_FromLoopbackWithoutForwardedProto_IsRedirectedToHttps()
    {
        // Control: proves the pipeline above really redirects, so the 200 above comes from trusting loopback
        using var client = await StartAsync(ReachableDatabase(), productionProxyPipeline: true);

        var response = await client.GetAsync("/health");

        ((int)response.StatusCode).Should().BeInRange(300, 399);
    }
}
