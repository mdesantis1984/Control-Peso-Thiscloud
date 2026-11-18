using Bunit;
using ControlPeso.Web.Components.Shared;
using ControlPeso.Web.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ControlPeso.Web.Tests.Components;

/// <summary>
/// S10: Google Analytics is rendered (and allowed by the CSP) only when a real measurement id is configured.
/// </summary>
public sealed class GoogleAnalyticsTests : TestContext
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("YOUR_GA_MEASUREMENT_ID", false)]
    [InlineData("YOUR_GA_MEASUREMENT_ID_HERE", false)]
    [InlineData("your_ga_id", false)]
    [InlineData("G-ABC123XYZ", true)]
    public void IsConfigured_ShouldRejectEmptyAndPlaceholderIds(string? id, bool expected)
    {
        GoogleAnalyticsSettings.IsConfigured(id).Should().Be(expected);
    }

    [Theory]
    [InlineData("G-ABC';alert(1);//")]
    [InlineData("G-ABC 123")]
    public void IsConfigured_ShouldRejectIdsWithUnsafeCharacters(string id)
    {
        GoogleAnalyticsSettings.IsConfigured(id).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("YOUR_GA_MEASUREMENT_ID")]
    public void Component_WhenNotConfigured_ShouldRenderNothing(string? id)
    {
        var cut = RenderComponent<GoogleAnalytics>(p => p.Add(c => c.MeasurementId, id));

        cut.Markup.Should().BeEmpty();
    }

    [Fact]
    public void Component_WhenConfigured_ShouldRenderGtagScriptWithTheId()
    {
        var cut = RenderComponent<GoogleAnalytics>(p => p.Add(c => c.MeasurementId, "G-ABC123XYZ"));

        cut.Markup.Should().Contain("https://www.googletagmanager.com/gtag/js?id=G-ABC123XYZ");
        cut.Markup.Should().Contain("gtag('config', 'G-ABC123XYZ'");
        cut.Markup.Should().NotContain("YOUR_");
    }

    private static async Task<string> GetCspAsync(string? measurementId)
    {
        var values = new Dictionary<string, string?> { ["GoogleAnalytics:MeasurementId"] = measurementId };
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var middleware = new SecurityHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLogger<SecurityHeadersMiddleware>.Instance,
            config);
        var context = new DefaultHttpContext();
        context.Request.Path = "/dashboard";

        await middleware.InvokeAsync(context);

        return context.Response.Headers["Content-Security-Policy"].ToString();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("YOUR_GA_MEASUREMENT_ID")]
    public async Task Csp_WhenAnalyticsNotConfigured_ShouldNotAllowGoogleDomains(string? id)
    {
        var csp = await GetCspAsync(id);

        csp.Should().NotContain("googletagmanager");
        csp.Should().NotContain("google-analytics");
        csp.Should().Contain("default-src 'self'");
    }

    [Fact]
    public async Task Csp_WhenAnalyticsConfigured_ShouldAllowGoogleDomainsInScriptConnectAndImg()
    {
        var csp = await GetCspAsync("G-ABC123XYZ");

        var directives = csp.Split("; ").ToDictionary(d => d.Split(' ')[0], d => d);
        directives["script-src"].Should().Contain("https://www.googletagmanager.com");
        directives["connect-src"].Should().Contain("https://*.google-analytics.com");
        directives["img-src"].Should().Contain("https://*.google-analytics.com");
        directives["img-src"].Should().Contain("https://www.googletagmanager.com");
    }
}
