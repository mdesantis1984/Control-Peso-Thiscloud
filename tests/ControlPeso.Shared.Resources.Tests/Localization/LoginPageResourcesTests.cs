using System.Globalization;
using ControlPeso.Shared.Resources.Localization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlPeso.Shared.Resources.Tests.Localization;

/// <summary>
/// The Login page resolves its localizer to <c>Components/Pages/Login</c> (namespace ControlPeso.Web.Components.Pages).
/// </summary>
public class LoginPageResourcesTests
{
    private static SharedResourceStringLocalizer CreateLoginLocalizer() =>
        new("Login", "Components/Pages/Login", new Mock<ILogger>().Object);

    [Theory]
    [InlineData("es-AR", "No pudimos iniciar sesión con tu cuenta. Probá de nuevo.")]
    [InlineData("en-US", "We couldn't sign you in. Please try again.")]
    [InlineData("fr-FR", "No pudimos iniciar sesión con tu cuenta. Probá de nuevo.")] // neutral fallback (es-AR texts)
    public void OAuthFailedMessage_ShouldResolveForCulture(string cultureName, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(cultureName);

            var result = CreateLoginLocalizer()["OAuthFailedMessage"];

            result.ResourceNotFound.Should().BeFalse();
            result.Value.Should().Be(expected);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
