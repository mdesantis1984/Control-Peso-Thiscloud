using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace ControlPeso.Web.Components.Pages;

public partial class Home
{
    [Inject]
    private IStringLocalizer<Home> Localizer { get; set; } = default!;

    // Page Metadata
    private string PageTitle => Localizer["PageTitle"];
    private string MetaDescription => Localizer["MetaDescription"];
    private string MetaKeywords => Localizer["MetaKeywords"];
    private string OgTitle => Localizer["OgTitle"];
    private string OgDescription => Localizer["OgDescription"];
    private string TwitterTitle => Localizer["TwitterTitle"];
    private string TwitterDescription => Localizer["TwitterDescription"];

    // Hero Section
    private string HeroTitle => Localizer["HeroTitle"];
    private string HeroSubtitle => Localizer["HeroSubtitle"];
    private string GoToDashboardButton => Localizer["GoToDashboardButton"];
    private string GetStartedButton => Localizer["GetStartedButton"];
    private string StatsBadge => Localizer["StatsBadge"];

    // Features
    private string FeatureFastRegistrationTitle => Localizer["FeatureFastRegistrationTitle"];
    private string FeatureFastRegistrationDescription => Localizer["FeatureFastRegistrationDescription"];
    private string FeatureAdvancedAnalysisTitle => Localizer["FeatureAdvancedAnalysisTitle"];
    private string FeatureAdvancedAnalysisDescription => Localizer["FeatureAdvancedAnalysisDescription"];
    private string FeatureMaxPrivacyTitle => Localizer["FeatureMaxPrivacyTitle"];
    private string FeatureMaxPrivacyDescription => Localizer["FeatureMaxPrivacyDescription"];
    private string FeatureAiPredictionsTitle => Localizer["FeatureAiPredictionsTitle"];
    private string FeatureAiPredictionsDescription => Localizer["FeatureAiPredictionsDescription"];
    private string FeatureMultiDeviceTitle => Localizer["FeatureMultiDeviceTitle"];
    private string FeatureMultiDeviceDescription => Localizer["FeatureMultiDeviceDescription"];
    private string FeaturePersonalizedGoalsTitle => Localizer["FeaturePersonalizedGoalsTitle"];
    private string FeaturePersonalizedGoalsDescription => Localizer["FeaturePersonalizedGoalsDescription"];
}
