using Bunit;
using Bunit.TestDoubles;
using ControlPeso.Shared.Resources.Extensions;
using ControlPeso.Web.Components.Pages;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace ControlPeso.Web.Tests.Pages;

/// <summary>
/// S13: every page has exactly one h1 (Home and Logout checked here; other pages in their own test classes).
/// </summary>
public sealed class PublicPagesHeadingTests : TestContext
{
    public PublicPagesHeadingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSharedResourcesLocalization();
        Services.AddLogging();
        Services.AddMudServices();
        this.AddTestAuthorization();
    }

    [Fact]
    public void Home_ShouldHaveExactlyOneH1()
    {
        RenderComponent<MudPopoverProvider>();

        var cut = RenderComponent<Home>();

        cut.FindAll("h1").Should().ContainSingle();
    }

    [Fact]
    public void Logout_ShouldHaveExactlyOneH1()
    {
        RenderComponent<MudPopoverProvider>();

        var cut = RenderComponent<Logout>();

        cut.FindAll("h1").Should().ContainSingle();
    }
}
