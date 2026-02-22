using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ControlPeso.Web.Components.Shared;

public partial class ConfirmationDialog
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }

    [Parameter] public string ContentText { get; set; } = "Are you sure?";
    [Parameter] public string ButtonText { get; set; } = "Confirm";
    [Parameter] public Color Color { get; set; } = Color.Primary;

    private void Cancel() => MudDialog?.Cancel();
    private void Confirm() => MudDialog?.Close(DialogResult.Ok(true));
}
