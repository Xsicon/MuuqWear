using Microsoft.AspNetCore.Components;
using MuuqWear.Web.Services;

namespace MuuqWear.Web.Components.Pages.AdminComponent.SystemTech;

public partial class AdminSystemIntegrationsTab
{
    [Inject] private AdminSystemTechIntegrationService IntegrationService { get; set; } = default!;

    private bool isLoading = true;
    private string? toast;
    private bool toastIsError;

    protected override async Task OnInitializedAsync()
    {
        await IntegrationService.RefreshIntegrationsAsync();
        isLoading = false;
    }

    private async Task TestConnection(string name)
    {
        toast = null;
        var (success, message) = await IntegrationService.TestIntegrationAsync(name);
        toastIsError = !success;
        toast = success
            ? $"Tested {name} connection."
            : AdminUiErrorHelper.FromApi(message, $"Failed to test {name} connection.");
        StateHasChanged();
    }

    private async Task Reconnect(string name)
    {
        toast = null;
        var (success, message) = await IntegrationService.ReconnectIntegrationAsync(name);
        toastIsError = !success;
        toast = success
            ? $"Reconnect attempted for {name}."
            : AdminUiErrorHelper.FromApi(message, $"Failed to reconnect {name}.");
        StateHasChanged();
    }
}
