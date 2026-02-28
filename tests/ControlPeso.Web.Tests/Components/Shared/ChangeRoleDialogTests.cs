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
/// Tests for ChangeRoleDialog component
/// </summary>
public sealed class ChangeRoleDialogTests : TestContext, IDisposable
{
    private readonly IStringLocalizer<ChangeRoleDialog> _localizer;

    public ChangeRoleDialogTests()
    {
        // Setup MudBlazor services
        Services.AddMudServices();

        // Setup mock localizer
        _localizer = Substitute.For<IStringLocalizer<ChangeRoleDialog>>();
        _localizer["DialogTitle"].Returns(new LocalizedString("DialogTitle", "Change User Role"));
        _localizer["ConfirmationMessage", Arg.Any<object[]>()].Returns(x =>
            new LocalizedString("ConfirmationMessage", $"Are you sure you want to change the role of {x.ArgAt<object[]>(0)[0]}?"));
        _localizer["NewRoleLabel"].Returns(new LocalizedString("NewRoleLabel", "New Role"));
        _localizer["RoleUser"].Returns(new LocalizedString("RoleUser", "User"));
        _localizer["RoleAdministrator"].Returns(new LocalizedString("RoleAdministrator", "Administrator"));
        _localizer["AuditWarning"].Returns(new LocalizedString("AuditWarning", "This action will be recorded in the audit log."));
        _localizer["CancelButton"].Returns(new LocalizedString("CancelButton", "CANCEL"));
        _localizer["ChangeRoleButton"].Returns(new LocalizedString("ChangeRoleButton", "CHANGE ROLE"));

        Services.AddSingleton(_localizer);
    }

    [Fact]
    public void ChangeRoleDialog_WhenRendered_ShouldShowDialogTitle()
    {
        // Arrange & Act
        var cut = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters
        {
            { nameof(ChangeRoleDialog.UserName), "Test User" },
            { nameof(ChangeRoleDialog.CurrentRole), UserRole.User }
        };

        var dialogService = Services.GetRequiredService<IDialogService>();
        IDialogReference? dialogReference = null;

        cut.InvokeAsync(async () =>
        {
            dialogReference = await dialogService.ShowAsync<ChangeRoleDialog>("", parameters);
        });

        cut.WaitForAssertion(() => cut.FindAll(".mud-dialog").Should().NotBeEmpty());

        // Assert
        cut.Find(".mud-dialog").InnerHtml.Should().Contain("Change User Role");
    }

    [Fact]
    public void ChangeRoleDialog_WhenRendered_ShouldShowConfirmationMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters
        {
            { nameof(ChangeRoleDialog.UserName), "Marco De Santis" },
            { nameof(ChangeRoleDialog.CurrentRole), UserRole.User }
        };

        var dialogService = Services.GetRequiredService<IDialogService>();
        cut.InvokeAsync(async () => await dialogService.ShowAsync<ChangeRoleDialog>("", parameters));

        cut.WaitForAssertion(() => cut.FindAll(".mud-dialog").Should().NotBeEmpty());

        // Assert
        cut.Markup.Should().Contain("Marco De Santis");
    }

    [Fact]
    public void ChangeRoleDialog_WhenCurrentRoleIsUser_ShouldSelectUserByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters
        {
            { nameof(ChangeRoleDialog.UserName), "Test User" },
            { nameof(ChangeRoleDialog.CurrentRole), UserRole.User }
        };

        var dialogService = Services.GetRequiredService<IDialogService>();
        cut.InvokeAsync(async () => await dialogService.ShowAsync<ChangeRoleDialog>("", parameters));

        cut.WaitForAssertion(() => cut.FindAll(".mud-dialog").Should().NotBeEmpty());

        // Assert - button should be disabled because current role equals selected role
        var changeButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("CHANGE ROLE"));
        changeButton.Should().NotBeNull();
        changeButton!.GetAttribute("disabled").Should().NotBeNull();
    }

    [Fact]
    public void ChangeRoleDialog_WhenRoleChanged_ShouldEnableSubmitButton()
    {
        // Arrange
        var cut = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters
        {
            { nameof(ChangeRoleDialog.UserName), "Test User" },
            { nameof(ChangeRoleDialog.CurrentRole), UserRole.User }
        };

        var dialogService = Services.GetRequiredService<IDialogService>();
        IDialogReference? dialogReference = null;

        cut.InvokeAsync(async () =>
        {
            dialogReference = await dialogService.ShowAsync<ChangeRoleDialog>("", parameters);
        });

        cut.WaitForAssertion(() => cut.FindAll(".mud-dialog").Should().NotBeEmpty());

        // Act - Select Administrator role
        var select = cut.FindComponent<MudSelect<UserRole>>();
        select.Instance.SelectOption(UserRole.Administrator);
        cut.WaitForState(() => select.Instance.Value == UserRole.Administrator);

        // Assert - button should be enabled
        var changeButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("CHANGE ROLE"));
        changeButton.Should().NotBeNull();
        changeButton!.GetAttribute("disabled").Should().BeNull();
    }

    [Fact]
    public void ChangeRoleDialog_WhenCancelClicked_ShouldCloseDialogWithCancelResult()
    {
        // Arrange
        var cut = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters
        {
            { nameof(ChangeRoleDialog.UserName), "Test User" },
            { nameof(ChangeRoleDialog.CurrentRole), UserRole.User }
        };

        var dialogService = Services.GetRequiredService<IDialogService>();
        IDialogReference? dialogReference = null;
        DialogResult? result = null;

        cut.InvokeAsync(async () =>
        {
            dialogReference = await dialogService.ShowAsync<ChangeRoleDialog>("", parameters);
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
    public void ChangeRoleDialog_WhenSubmitClicked_ShouldReturnSelectedRole()
    {
        // Arrange
        var cut = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters
        {
            { nameof(ChangeRoleDialog.UserName), "Test User" },
            { nameof(ChangeRoleDialog.CurrentRole), UserRole.User }
        };

        var dialogService = Services.GetRequiredService<IDialogService>();
        IDialogReference? dialogReference = null;
        DialogResult? result = null;

        cut.InvokeAsync(async () =>
        {
            dialogReference = await dialogService.ShowAsync<ChangeRoleDialog>("", parameters);
        });

        cut.WaitForAssertion(() => cut.FindAll(".mud-dialog").Should().NotBeEmpty());

        // Act - Change role to Administrator
        var select = cut.FindComponent<MudSelect<UserRole>>();
        select.Instance.SelectOption(UserRole.Administrator);
        cut.WaitForState(() => select.Instance.Value == UserRole.Administrator);

        var submitButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("CHANGE ROLE"));
        submitButton.Should().NotBeNull();

        cut.InvokeAsync(async () =>
        {
            submitButton!.Click();
            result = await dialogReference!.Result;
        });

        cut.WaitForAssertion(() => result.Should().NotBeNull());

        // Assert
        result!.Canceled.Should().BeFalse();
        result.Data.Should().Be(UserRole.Administrator);
    }

    [Fact]
    public void ChangeRoleDialog_WhenRendered_ShouldShowAuditWarningMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters
        {
            { nameof(ChangeRoleDialog.UserName), "Test User" },
            { nameof(ChangeRoleDialog.CurrentRole), UserRole.User }
        };

        var dialogService = Services.GetRequiredService<IDialogService>();
        cut.InvokeAsync(async () => await dialogService.ShowAsync<ChangeRoleDialog>("", parameters));

        cut.WaitForAssertion(() => cut.FindAll(".mud-dialog").Should().NotBeEmpty());

        // Assert
        cut.Markup.Should().Contain("This action will be recorded in the audit log");
    }

    public new void Dispose()
    {
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
