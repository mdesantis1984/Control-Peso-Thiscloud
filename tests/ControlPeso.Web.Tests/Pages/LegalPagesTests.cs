using System.Globalization;
using Bunit;
using ControlPeso.Web.Pages.Legal;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace ControlPeso.Web.Tests.Pages;

/// <summary>
/// S13: legal pages render dates with the culture of the displayed language, expose exactly one h1,
/// and their icon-only language switch has an accessible name.
/// </summary>
public sealed class LegalPagesTests : TestContext
{
    private readonly CultureInfo _previousCulture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _previousUiCulture = CultureInfo.CurrentUICulture;

    public LegalPagesTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentCulture = _previousCulture;
        CultureInfo.CurrentUICulture = _previousUiCulture;
        base.Dispose(disposing);
    }

    private IRenderedComponent<TPage> Render<TPage>(string culture) where TPage : Microsoft.AspNetCore.Components.IComponent
    {
        var info = CultureInfo.GetCultureInfo(culture);
        CultureInfo.CurrentCulture = info;
        CultureInfo.CurrentUICulture = info;
        RenderComponent<MudPopoverProvider>();
        return RenderComponent<TPage>();
    }

    private static string ExpectedDate(string culture) =>
        new DateTime(2026, 2, 25).ToString("D", CultureInfo.GetCultureInfo(culture));

    [Theory]
    [InlineData("es-AR")]
    [InlineData("en-US")]
    public void PrivacyPolicy_ShouldRenderDatesInTheCurrentCulture(string culture)
    {
        var cut = Render<PrivacyPolicy>(culture);

        cut.Markup.Should().Contain(ExpectedDate(culture));
        if (culture == "es-AR")
        {
            cut.Markup.Should().NotContain("February 25, 2026");
        }
    }

    [Theory]
    [InlineData("es-AR")]
    [InlineData("en-US")]
    public void TermsAndConditions_ShouldRenderDatesInTheCurrentCulture(string culture)
    {
        var cut = Render<TermsAndConditions>(culture);

        cut.Markup.Should().Contain(ExpectedDate(culture));
        if (culture == "es-AR")
        {
            cut.Markup.Should().NotContain("February 25, 2026");
        }
    }

    [Theory]
    [InlineData("es-AR", "Fecha de Vigencia", "25 de febrero de 2026")]
    [InlineData("en-US", "Effective Date", "February 25, 2026")]
    public void PrivacyPolicy_ShouldUseTheLongDatePatternOfTheCulture(string culture, string label, string datePart)
    {
        var cut = Render<PrivacyPolicy>(culture);

        cut.Markup.Should().Contain(label);
        cut.Markup.Should().Contain(datePart);
    }

    [Fact]
    public void LegalDateFormatter_ShouldFallBackToTheLanguageDefaultCulture_WhenCurrentCultureDiffers()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

        LegalDateFormatter.Format(LegalDateFormatter.DocumentDate, isEnglish: true).Should().Contain("February 25, 2026");
        LegalDateFormatter.Format(LegalDateFormatter.DocumentDate, isEnglish: false).Should().Contain("25 de febrero de 2026");
    }

    [Theory]
    [InlineData("es-AR", "19 de noviembre de 2026")]
    [InlineData("en-US", "November 19, 2026")]
    public void Changelog_ShouldShowTheCurrentVersionWithItsReleaseDate(string culture, string datePart)
    {
        var cut = Render<Changelog>(culture);

        cut.Markup.Should().Contain("v1.3.0 - ");
        cut.Markup.Should().Contain(datePart);
        cut.Markup.Should().Contain("v1.3.0-initial - February 25, 2026");
        cut.Markup.Should().Contain("PostgreSQL 15");
        cut.Markup.Should().Contain("SkiaSharp");
    }

    [Theory]
    [InlineData("es-AR")]
    [InlineData("en-US")]
    public void ThirdPartyLicenses_ShouldListSkiaSharpAndPostgreSql_WithoutLegacyProviders(string culture)
    {
        var cut = Render<ThirdPartyLicenses>(culture);

        cut.Markup.Should().Contain("SkiaSharp");
        cut.Markup.Should().Contain("Npgsql.EntityFrameworkCore.PostgreSQL");
        cut.Markup.Should().NotContain("ImageSharp");
        cut.Markup.Should().NotContain("SQL Server");
        cut.Markup.Should().NotContain("SQLite");
        cut.Markup.Should().NotContain("Telegram");
    }

    [Theory]
    [InlineData("es-AR", "PostgreSQL con controles de acceso")]
    [InlineData("en-US", "PostgreSQL with access controls")]
    public void PrivacyPolicy_ShouldDescribeThePostgreSqlDatabase(string culture, string expected)
    {
        var cut = Render<PrivacyPolicy>(culture);

        cut.Markup.Should().Contain(expected);
        cut.Markup.Should().NotContain("SQL Server");
    }

    [Theory]
    [InlineData(typeof(PrivacyPolicy))]
    [InlineData(typeof(TermsAndConditions))]
    [InlineData(typeof(ThirdPartyLicenses))]
    [InlineData(typeof(Changelog))]
    public void LegalPages_ShouldHaveExactlyOneH1_AndAnAccessibleLanguageSwitch(Type pageType)
    {
        var method = typeof(LegalPagesTests).GetMethod(nameof(Render), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .MakeGenericMethod(pageType);
        var cut = (IRenderedFragment)method.Invoke(this, ["es-AR"])!;

        cut.FindAll("h1").Should().ContainSingle();

        // Every button must expose an accessible name (aria-label or text content)
        foreach (var button in cut.FindAll("button, a.mud-icon-button"))
        {
            var name = button.GetAttribute("aria-label") ?? button.TextContent.Trim();
            name.Should().NotBeNullOrWhiteSpace($"button '{button.OuterHtml[..Math.Min(120, button.OuterHtml.Length)]}' needs an accessible name");
        }
    }
}
