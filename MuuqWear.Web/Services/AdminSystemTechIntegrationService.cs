using MuuqWear.Application.Services.AdminSystemService;
using MuuqWear.Model.DTO.AdminSystem;

namespace MuuqWear.Web.Services;

public class AdminSystemTechIntegrationService
{
    private readonly IAdminSystemService _adminSystemService;
    private readonly HashSet<string> _loadingIntegrations = new(StringComparer.OrdinalIgnoreCase);

    private List<IntegrationStatusModel> _integrations = [];

    public AdminSystemTechIntegrationService(IAdminSystemService adminSystemService)
    {
        _adminSystemService = adminSystemService;
    }

    public SystemHealthOverviewModel? Overview { get; private set; }

    public IReadOnlyList<IntegrationStatusModel> Integrations => _integrations;

    public string? LastError { get; private set; }

    public bool IsIntegrationLoading(string name) =>
        _loadingIntegrations.Contains(name);

    public async Task RefreshOverviewAsync()
    {
        LastError = null;
        var result = await _adminSystemService.GetOverviewAsync();

        if (result.Success && result.Data != null)
            Overview = result.Data;
        else
            LastError = result.Message;
    }

    public async Task RefreshIntegrationsAsync()
    {
        LastError = null;
        var result = await _adminSystemService.GetIntegrationsAsync();

        if (result.Success && result.Data != null)
            _integrations = result.Data;
        else
            LastError = result.Message;
    }

    public async Task RefreshHealthAsync() => await RefreshOverviewAsync();

    public async Task<(bool Success, string Message)> TestIntegrationAsync(string name)
    {
        LastError = null;
        _loadingIntegrations.Add(name);

        try
        {
            var result = await _adminSystemService.TestIntegrationAsync(name);

            if (result.Success && result.Data != null)
                UpsertIntegration(result.Data);

            if (!result.Success)
                LastError = result.Message;

            return (result.Success, result.Message);
        }
        finally
        {
            _loadingIntegrations.Remove(name);
        }
    }

    public async Task<(bool Success, string Message)> ReconnectIntegrationAsync(string name)
    {
        LastError = null;
        _loadingIntegrations.Add(name);

        try
        {
            var result = await _adminSystemService.ReconnectIntegrationAsync(name);

            if (result.Success && result.Data != null)
                UpsertIntegration(result.Data);

            if (!result.Success)
                LastError = result.Message;

            return (result.Success, result.Message);
        }
        finally
        {
            _loadingIntegrations.Remove(name);
        }
    }

    private void UpsertIntegration(IntegrationStatusModel updated)
    {
        var index = _integrations.FindIndex(i =>
            i.Name.Equals(updated.Name, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
            _integrations[index] = updated;
        else
            _integrations.Add(updated);
    }
}
