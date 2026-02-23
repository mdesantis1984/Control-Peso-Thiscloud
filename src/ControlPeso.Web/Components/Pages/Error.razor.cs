using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using System.Diagnostics;

namespace ControlPeso.Web.Components.Pages;

/// <summary>
/// Error page code-behind with localized strings.
/// </summary>
public partial class Error
{
    [Inject]
    private IStringLocalizer<Error> Localizer { get; set; } = null!;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string? RequestId { get; set; }

    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    // Localized Properties - Page Metadata
    private string PageTitle => Localizer[nameof(PageTitle)];

    // Localized Properties - Error Content
    private string ErrorTitle => Localizer[nameof(ErrorTitle)];
    private string ErrorMessage => Localizer[nameof(ErrorMessage)];
    private string RequestIdLabel => Localizer[nameof(RequestIdLabel)];

    // Localized Properties - Development Mode
    private string DevelopmentModeTitle => Localizer[nameof(DevelopmentModeTitle)];
    private MarkupString DevelopmentModeDescription => (MarkupString)Localizer[nameof(DevelopmentModeDescription)].Value;
    private MarkupString DevelopmentModeWarning => (MarkupString)Localizer[nameof(DevelopmentModeWarning)].Value;

    protected override void OnInitialized() =>
        RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
}
