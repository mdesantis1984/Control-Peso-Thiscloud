using Bunit;
using Bunit.TestDoubles;
using ControlPeso.Shared.Resources.Extensions;
using ControlPeso.Web.Components.Layout;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using NSubstitute;

namespace ControlPeso.Web.Tests.Components;

/// <summary>
/// S12: demo/test pages have no navigation links, and diagnostics links are shown only to
/// administrators and only in the Development environment.
/// </summary>
public sealed class NavMenuTests : TestContext
{
    private IRenderedComponent<NavMenu> Render(string environment, bool isAdmin)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(environment);

        Services.AddSharedResourcesLocalization();
        Services.AddLogging();
        Services.AddSingleton(env);
        Services.AddSingleton<IHostEnvironment>(env);
        Services.AddMudServices();

        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("test");
        if (isAdmin)
        {
            auth.SetRoles("Administrator");
        }

        RenderComponent<MudBlazor.MudPopoverProvider>();
        return RenderComponent<NavMenu>();
    }

    [Theory]
    [InlineData("Production", true)]
    [InlineData("Production", false)]
    [InlineData("Development", true)]
    [InlineData("Development", false)]
    public void Menu_ShouldNeverLinkToDemoOrTestPages(string environment, bool isAdmin)
    {
        var cut = Render(environment, isAdmin);

        cut.Markup.Should().NotContain("/test-badges");
        cut.Markup.Should().NotContain("/test-flags");
        cut.Markup.Should().NotContain("/counter");
        cut.Markup.Should().NotContain("/weather");
    }

    [Theory]
    [InlineData("Production", true)]
    [InlineData("Production", false)]
    [InlineData("Development", false)]
    public void Menu_ShouldHideDiagnosticsLinks_UnlessAdministratorInDevelopment(string environment, bool isAdmin)
    {
        var cut = Render(environment, isAdmin);

        cut.Markup.Should().NotContain("/diag-auth");
        cut.Markup.Should().NotContain("/diagnostics/telegram");
    }

    [Fact]
    public void Menu_ShouldShowDiagnosticsLinks_ToAdministratorInDevelopment()
    {
        var cut = Render("Development", isAdmin: true);

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("/diag-auth");
            cut.Markup.Should().Contain("/diagnostics/telegram");
        });
    }

    [Fact]
    public void Menu_ShouldKeepAdminPanelLink_ForAdministratorInProduction()
    {
        var cut = Render("Production", isAdmin: true);

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("href=\"/admin\""));
    }
}
