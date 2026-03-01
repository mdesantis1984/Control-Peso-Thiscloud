using ControlPeso.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace ControlPeso.Web.Components.Shared;

public partial class ChangeStatusDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IStringLocalizer<ChangeStatusDialog> Localizer { get; set; } = default!;

    [Parameter, EditorRequired] public string UserName { get; set; } = string.Empty;
    [Parameter, EditorRequired] public UserStatus CurrentStatus { get; set; }

    private UserStatus _selectedStatus;

    // Localized strings
    private string DialogTitle => Localizer["DialogTitle"];
    private string ConfirmationMessage => Localizer["ConfirmationMessage", UserName];
    private string NewStatusLabel => Localizer["NewStatusLabel"];
    private string StatusActive => Localizer["StatusActive"];
    private string StatusInactive => Localizer["StatusInactive"];
    private string StatusPending => Localizer["StatusPending"];
    private string CancelButton => Localizer["CancelButton"];
    private string ChangeStatusButton => Localizer["ChangeStatusButton"];

    // Alert messages
    private string AlertInactive => Localizer["AlertInactive"];
    private string AlertActive => Localizer["AlertActive"];
    private string AlertPending => Localizer["AlertPending"];
    private string AlertAudit => Localizer["AlertAudit"];

    protected override void OnInitialized()
    {
        _selectedStatus = CurrentStatus;
    }

    private void Submit()
    {
        MudDialog.Close(DialogResult.Ok(_selectedStatus));
    }

    private void Cancel()
    {
        MudDialog.Close(DialogResult.Cancel());
    }

    private Severity GetAlertSeverity()
    {
        return _selectedStatus switch
        {
            UserStatus.Inactive => Severity.Warning,
            UserStatus.Active => Severity.Success,
            UserStatus.Pending => Severity.Info,
            _ => Severity.Normal
        };
    }

    private string GetAlertText()
    {
        return _selectedStatus switch
        {
            UserStatus.Inactive => AlertInactive,
            UserStatus.Active => AlertActive,
            UserStatus.Pending => AlertPending,
            _ => AlertAudit
        };
    }

    private Color GetButtonColor()
    {
        return _selectedStatus switch
        {
            UserStatus.Inactive => Color.Error,
            UserStatus.Active => Color.Success,
            UserStatus.Pending => Color.Warning,
            _ => Color.Primary
        };
    }
}
