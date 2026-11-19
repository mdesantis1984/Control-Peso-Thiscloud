using ControlPeso.Web.Extensions;
using FluentAssertions;

namespace ControlPeso.Web.Tests.Extensions;

public sealed class EndpointExtensionsTests
{
    [Fact]
    public void CreateChallengeProperties_MakesTheSignInCookiePersistent()
    {
        var properties = EndpointExtensions.CreateChallengeProperties("/dashboard");

        properties.IsPersistent.Should().BeTrue();
        properties.RedirectUri.Should().Be("/dashboard");
    }

    [Theory]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("/history?page=2", "/history?page=2")]
    [InlineData("https://evil.example/phish", "/")]
    [InlineData("//evil.example", "/")]
    [InlineData("/\\evil.example", "/")]
    [InlineData("javascript:alert(1)", "/")]
    public void CreateChallengeProperties_OnlyRedirectsToLocalPaths(string? returnUrl, string expected)
    {
        EndpointExtensions.CreateChallengeProperties(returnUrl).RedirectUri.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "/login")]
    [InlineData("/privacy", "/privacy")]
    [InlineData("https://evil.example", "/login")]
    [InlineData("//evil.example", "/login")]
    public void ToLocalUrl_FallsBackForExternalUrls(string? returnUrl, string expected)
    {
        EndpointExtensions.ToLocalUrl(returnUrl, "/login").Should().Be(expected);
    }
}
