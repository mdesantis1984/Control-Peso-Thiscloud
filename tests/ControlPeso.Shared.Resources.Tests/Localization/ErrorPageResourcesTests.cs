using System.Globalization;
using ControlPeso.Shared.Resources.Localization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlPeso.Shared.Resources.Tests.Localization;

/// <summary>
/// The Error page resolves its localizer to <c>Components/Pages/Error</c> (namespace ControlPeso.Web.Components.Pages).
/// These tests prove the embedded resources exist under that exact name for every supported culture.
/// </summary>
public class ErrorPageResourcesTests
{
    private static SharedResourceStringLocalizer CreateErrorLocalizer() =>
        new("Error", "Components/Pages/Error", new Mock<ILogger>().Object);

    [Theory]
    [InlineData("es-AR", "Ocurrió un error al procesar tu solicitud.")]
    [InlineData("en-US", "An error occurred while processing your request.")]
    [InlineData("fr-FR", "Ocurrió un error al procesar tu solicitud.")] // neutral fallback (es-AR texts)
    public void ErrorMessage_ShouldResolveFromComponentsPagesError_ForCulture(string cultureName, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(cultureName);

            var result = CreateErrorLocalizer()["ErrorMessage"];

            result.ResourceNotFound.Should().BeFalse();
            result.Value.Should().Be(expected);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
