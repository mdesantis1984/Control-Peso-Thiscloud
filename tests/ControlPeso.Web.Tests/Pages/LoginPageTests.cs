using System.Globalization;
using Bunit;
using ControlPeso.Shared.Resources.Extensions;
using ControlPeso.Web.Components.Pages;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace ControlPeso.Web.Tests.Pages;

/// <summary>
/// S7 (login side): an OAuth failure redirect (/login?error=oauth_failed) shows a localized alert.
/// </summary>
public sealed class LoginPageTests : TestContext
{
    private readonly CultureInfo _previousCulture = CultureInfo.CurrentUICulture;

    public LoginPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSharedResourcesLocalization();
        Services.AddLogging();
        Services.AddMudServices();
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentUICulture = _previousCulture;
        base.Dispose(disposing);
    }

    private IRenderedComponent<Login> Render(string culture, string query)
    {
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
            .NavigateTo("http://localhost/login" + query);
        RenderComponent<MudPopoverProvider>();
        return RenderComponent<Login>();
    }

    [Theory]
    [InlineData("es-AR", "No pudimos iniciar sesión con tu cuenta. Probá de nuevo.")]
    [InlineData("en-US", "We couldn't sign you in. Please try again.")]
    public void WhenOAuthFailed_ShouldShowLocalizedAlert(string culture, string expected)
    {
        var cut = Render(culture, "?error=oauth_failed");

        var alert = cut.Find("[role='alert'].oauth-error-alert, .oauth-error-alert");
        alert.TextContent.Trim().Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?returnUrl=%2Fdashboard")]
    [InlineData("?error=")]
    public void WhenNoError_ShouldNotShowAlert(string query)
    {
        var cut = Render("es-AR", query);

        cut.FindAll(".oauth-error-alert").Should().BeEmpty();
        cut.Markup.Should().Contain("login-google-button");
    }

    [Fact]
    public void ShouldRenderExactlyOneH1()
    {
        var cut = Render("es-AR", "");

        cut.FindAll("h1").Should().ContainSingle();
    }

    [Theory]
    [InlineData("es-AR")]
    [InlineData("en-US")]
    public void ShouldOfferGoogleOnly_WithoutAnyLinkedInOption(string culture)
    {
        var cut = Render(culture, "");

        cut.Markup.Should().Contain("login-google-button");
        cut.Markup.Should().NotContainEquivalentOf("linkedin");
        cut.FindAll("button, a[href]").Should().NotContain(e => e.OuterHtml.Contains("linkedin", StringComparison.OrdinalIgnoreCase));
    }
}
