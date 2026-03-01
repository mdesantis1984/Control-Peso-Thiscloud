using ControlPeso.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace ControlPeso.Web.Components.Shared;

public partial class ChangeRoleDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IStringLocalizer<ChangeRoleDialog> Localizer { get; set; } = default!;

    [Parameter, EditorRequired] public string UserName { get; set; } = string.Empty;
    [Parameter, EditorRequired] public UserRole CurrentRole { get; set; }

    private UserRole _selectedRole;

    // Localized strings
    private string DialogTitle => Localizer["DialogTitle"];
    private string ConfirmationMessage => Localizer["ConfirmationMessage", UserName];
    private string NewRoleLabel => Localizer["NewRoleLabel"];
    private string RoleUser => Localizer["RoleUser"];
    private string RoleAdministrator => Localizer["RoleAdministrator"];
    private string AuditWarning => Localizer["AuditWarning"];
    private string CancelButton => Localizer["CancelButton"];
    private string ChangeRoleButton => Localizer["ChangeRoleButton"];

    protected override void OnInitialized()
    {
        _selectedRole = CurrentRole;
    }

    private void Submit()
    {
        MudDialog.Close(DialogResult.Ok(_selectedRole));
    }

    private void Cancel()
    {
        MudDialog.Close(DialogResult.Cancel());
    }
}
