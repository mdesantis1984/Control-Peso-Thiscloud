using Bunit;
using ControlPeso.Domain.Enums;
using ControlPeso.Web.Components.Shared;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;

namespace ControlPeso.Web.Tests.Components.Shared;

/// <summary>
/// Tests for ChangeStatusDialog component
/// </summary>
public sealed class ChangeStatusDialogTests : TestContext, IDisposable
{
    private readonly IStringLocalizer<ChangeStatusDialog> _localizer;

    public ChangeStatusDialogTests()
    {
        // Setup MudBlazor services
        Services.AddMudServices();

        // Setup JSInterop for MudBlazor components
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Setup mock localizer
        _localizer = Substitute.For<IStringLocalizer<ChangeStatusDialog>>();
        _localizer["DialogTitle"].Returns(new LocalizedString("DialogTitle", "Change User Status"));
        _localizer["ConfirmationMessage", Arg.Any<object[]>()].Returns(x =>
            new LocalizedString("ConfirmationMessage", $"Are you sure you want to change the status of {x.ArgAt<object[]>(0)[0]}?"));
        _localizer["NewStatusLabel"].Returns(new LocalizedString("NewStatusLabel", "New Status"));
        _localizer["StatusActive"].Returns(new LocalizedString("StatusActive", "Active"));
        _localizer["StatusInactive"].Returns(new LocalizedString("StatusInactive", "Inactive"));
        _localizer["StatusPending"].Returns(new LocalizedString("StatusPending", "Pending"));
        _localizer["AlertInactive"].Returns(new LocalizedString("AlertInactive", "The user will not be able to access the application while inactive."));
        _localizer["AlertActive"].Returns(new LocalizedString("AlertActive", "The user will be able to access the application normally."));
        _localizer["AlertPending"].Returns(new LocalizedString("AlertPending", "The user will be awaiting approval."));
        _localizer["AlertAudit"].Returns(new LocalizedString("AlertAudit", "This action will be recorded in the audit log."));
        _localizer["CancelButton"].Returns(new LocalizedString("CancelButton", "CANCEL"));
        _localizer["ChangeStatusButton"].Returns(new LocalizedString("ChangeStatusButton", "CHANGE STATUS"));

        Services.AddSingleton(_localizer);
    }

    [Fact]
    public void ChangeStatusDialog_WhenRendered_ShouldShowDialogTitle()
    {
        // Arrange & Act
        var cut = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters
        {
            { nameof(ChangeStatusDialog.UserName), "Test User" },
            { nameof(ChangeStatusDialog.CurrentStatus), UserStatus.Active }
        };

        var dialogService = Services.GetRequiredService<IDialogService>();
        cut.InvokeAsync(async () => await dialogService.ShowAsync<ChangeStatusDialog>("", parameters));

        cut.WaitForAssertion(() => cut.FindAll(".mud-dialog").Should().NotBeEmpty());

        // Assert
        cut.Find(".mud-dialog").InnerHtml.Should().Contain("Change User Status");
    }

    [Fact]
    public void ChangeStatusDialog_WhenRendered_ShouldShowConfirmationMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters
        {
            { nameof(ChangeStatusDialog.UserName), "Marco De Santis" },
            { nameof(ChangeStatusDialog.CurrentStatus), UserStatus.Active }
        };

        var dialogService = Services.GetRequiredService<IDialogService>();
        cut.InvokeAsync(async () => await dialogService.ShowAsync<ChangeStatusDialog>("", parameters));

        cut.WaitForAssertion(() => cut.FindAll(".mud-dialog").Should().NotBeEmpty());

        // Assert
        cut.Markup.Should().Contain("Marco De Santis");
    }

    [Fact]
    public void ChangeStatusDialog_WhenCurrentStatusIsActive_ShouldSelectActiveByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters
        {
            { nameof(ChangeStatusDialog.UserName), "Test User" },
            { nameof(ChangeStatusDialog.CurrentStatus), UserStatus.Active }
        };

        var dialogService = Services.GetRequiredService<IDialogService>();
        cut.InvokeAsync(async () => await dialogService.ShowAsync<ChangeStatusDialog>("", parameters));

        cut.WaitForAssertion(() => cut.FindAll(".mud-dialog").Should().NotBeEmpty());

        // Assert - button should be disabled because current status equals selected status
        var changeButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("CHANGE STATUS"));
        changeButton.Should().NotBeNull();
        changeButton!.GetAttribute("disabled").Should().NotBeNull();
    }

    [Fact]
    public void ChangeStatusDialog_WhenStatusChanged_ShouldEnableSubmitButton()
    {
        // Arrange
        var cut = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters
        {
            { nameof(ChangeStatusDialog.UserName), "Test User" },
            { nameof(ChangeStatusDialog.CurrentStatus), UserStatus.Active }
        };

        var dialogService = Services.GetRequiredService<IDialogService>();
        cut.InvokeAsync(async () => await dialogService.ShowAsync<ChangeStatusDialog>("", parameters));

        cut.WaitForAssertion(() => cut.FindAll(".mud-dialog").Should().NotBeEmpty());

        // Act - Select Inactive status
        var select = cut.FindComponent<MudSelect<UserStatus>>();
        select.Instance.SelectOption(UserStatus.Inactive);
        cut.WaitForState(() => select.Instance.Value == UserStatus.Inactive);

        // Assert - button should be enabled
        var changeButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("CHANGE STATUS"));
        changeButton.Should().NotBeNull();
        changeButton!.GetAttribute("disabled").Should().BeNull();
    }

    [Fact]
    public void ChangeStatusDialog_WhenInactiveSelected_ShouldShowWarningAlert()
    {
        // Arrange
        var cut = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters
        {
            { nameof(ChangeStatusDialog.UserName), "Test User" },
            { nameof(ChangeStatusDialog.CurrentStatus), UserStatus.Active }
        };

        var dialogService = Services.GetRequiredService<IDialogService>();
        cut.InvokeAsync(async () => await dialogService.ShowAsync<ChangeStatusDialog>("", parameters));

        cut.WaitForAssertion(() => cut.FindAll(".mud-dialog").Should().NotBeEmpty());

        // Act
        var select = cut.FindComponent<MudSelect<UserStatus>>();
        select.Instance.SelectOption(UserStatus.Inactive);
        cut.WaitForState(() => select.Instance.Value == UserStatus.Inactive);

        // Assert
        cut.Markup.Should().Contain("The user will not be able to access the application while inactive");
    }

    [Fact]
    public void ChangeStatusDialog_WhenActiveSelected_ShouldShowSuccessAlert()
    {
        // Arrange
        var cut = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters
        {
            { nameof(ChangeStatusDialog.UserName), "Test User" },
            { nameof(ChangeStatusDialog.CurrentStatus), UserStatus.Inactive }
        };

        var dialogService = Services.GetRequiredService<IDialogService>();
        cut.InvokeAsync(async () => await dialogService.ShowAsync<ChangeStatusDialog>("", parameters));

        cut.WaitForAssertion(() => cut.FindAll(".mud-dialog").Should().NotBeEmpty());

        // Act
        var select = cut.FindComponent<MudSelect<UserStatus>>();
        select.Instance.SelectOption(UserStatus.Active);
        cut.WaitForState(() => select.Instance.Value == UserStatus.Active);

        // Assert
        cut.Markup.Should().Contain("The user will be able to access the application normally");
    }

    [Fact]
    public void ChangeStatusDialog_WhenPendingSelected_ShouldShowInfoAlert()
    {
        // Arrange
        var cut = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters
        {
            { nameof(ChangeStatusDialog.UserName), "Test User" },
            { nameof(ChangeStatusDialog.CurrentStatus), UserStatus.Active }
        };

        var dialogService = Services.GetRequiredService<IDialogService>();
        cut.InvokeAsync(async () => await dialogService.ShowAsync<ChangeStatusDialog>("", parameters));

        cut.WaitForAssertion(() => cut.FindAll(".mud-dialog").Should().NotBeEmpty());

        // Act
        var select = cut.FindComponent<MudSelect<UserStatus>>();
        select.Instance.SelectOption(UserStatus.Pending);
        cut.WaitForState(() => select.Instance.Value == UserStatus.Pending);

        // Assert
        cut.Markup.Should().Contain("The user will be awaiting approval");
    }

    [Fact]
    public void ChangeStatusDialog_WhenCancelClicked_ShouldCloseDialogWithCancelResult()
    {
        // Arrange
        var cut = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters
        {
            { nameof(ChangeStatusDialog.UserName), "Test User" },
            { nameof(ChangeStatusDialog.CurrentStatus), UserStatus.Active }
        };

        var dialogService = Services.GetRequiredService<IDialogService>();
        IDialogReference? dialogReference = null;
        DialogResult? result = null;

        cut.InvokeAsync(async () =>
        {
            dialogReference = await dialogService.ShowAsync<ChangeStatusDialog>("", parameters);
            result = await dialogReference.Result;
        });

        cut.WaitForAssertion(() => cut.FindAll(".mud-dialog").Should().NotBeEmpty());

        // Act
        var cancelButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("CANCEL"));
        cancelButton.Should().NotBeNull();
        cancelButton!.Click();

        cut.WaitForAssertion(() => result.Should().NotBeNull());

        // Assert
        result!.Canceled.Should().BeTrue();
    }

    [Fact]
    public void ChangeStatusDialog_WhenSubmitClicked_ShouldReturnSelectedStatus()
    {
        // Arrange
        var cut = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters
        {
            { nameof(ChangeStatusDialog.UserName), "Test User" },
            { nameof(ChangeStatusDialog.CurrentStatus), UserStatus.Active }
        };

        var dialogService = Services.GetRequiredService<IDialogService>();
        IDialogReference? dialogReference = null;
        DialogResult? result = null;

        cut.InvokeAsync(async () =>
        {
            dialogReference = await dialogService.ShowAsync<ChangeStatusDialog>("", parameters);
        });

        cut.WaitForAssertion(() => cut.FindAll(".mud-dialog").Should().NotBeEmpty());

        // Act - Change status to Inactive
        var select = cut.FindComponent<MudSelect<UserStatus>>();
        select.Instance.SelectOption(UserStatus.Inactive);
        cut.WaitForState(() => select.Instance.Value == UserStatus.Inactive);

        var submitButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("CHANGE STATUS"));
        submitButton.Should().NotBeNull();

        cut.InvokeAsync(async () =>
        {
            submitButton!.Click();
            result = await dialogReference!.Result;
        });

        cut.WaitForAssertion(() => result.Should().NotBeNull());

        // Assert
        result!.Canceled.Should().BeFalse();
        result.Data.Should().Be(UserStatus.Inactive);
    }

    public new void Dispose()
    {
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
