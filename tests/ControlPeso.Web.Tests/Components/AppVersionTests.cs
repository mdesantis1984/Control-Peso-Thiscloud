using System.Globalization;
using System.Reflection;
using Bunit;
using Bunit.TestDoubles;
using ControlPeso.Shared.Resources.Extensions;
using ControlPeso.Web.Components.Layout;
using ControlPeso.Web.Components.Shared;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using NSubstitute;

namespace ControlPeso.Web.Tests.Components;

/// <summary>
/// S13: one version source (assembly informational version) for sidebar and footer.
/// </summary>
public sealed class AppVersionTests : TestContext
{
    private static string ExpectedVersion()
    {
        var info = typeof(AppVersionInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        return info.Split('+')[0];
    }

    [Fact]
    public void Current_ShouldBeTheInformationalVersionWithoutBuildMetadata()
    {
        AppVersionInfo.Current.Should().Be(ExpectedVersion());
        AppVersionInfo.Current.Should().NotContain("+");
        AppVersionInfo.Current.Should().MatchRegex(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$", "MinVer derives a SemVer 2.0 version from the release tags");
    }

    [Fact]
    public void Footer_ShouldShowTheAssemblyVersion_NotAHardCodedOne()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        RenderComponent<MudBlazor.MudPopoverProvider>();

        var cut = RenderComponent<LegalFooter>();

        cut.Markup.Should().Contain($"v{AppVersionInfo.Current}");
    }

    [Fact]
    public void Sidebar_ShouldShowTheSameVersionAsTheFooter()
    {
        // The sidebar text comes from culture-specific resources only (es-AR, en-US);
        // pin the culture so the test does not depend on the machine locale.
        var previousUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es-AR");
        try
        {
            RenderSidebarAndAssertVersion();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    private void RenderSidebarAndAssertVersion()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns("Production");
        Services.AddSharedResourcesLocalization();
        Services.AddLogging();
        Services.AddSingleton(env);
        Services.AddMudServices();
        this.AddTestAuthorization().SetAuthorized("test");
        RenderComponent<MudBlazor.MudPopoverProvider>();

        var cut = RenderComponent<NavMenu>();

        cut.Markup.Should().Contain($"Control Peso Thiscloud v{AppVersionInfo.Current}");
        cut.Markup.Should().NotContain("v1.0<");
    }
}
