using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ControlPeso.Web.Pages;

public partial class Trends
{
    [Inject] private ILogger<Trends> Logger { get; set; } = null!;

    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("Loading trends analysis");
        
        // TODO: Implementar ITrendService
        await Task.Delay(500);
        
        _isLoading = false;
    }
}
