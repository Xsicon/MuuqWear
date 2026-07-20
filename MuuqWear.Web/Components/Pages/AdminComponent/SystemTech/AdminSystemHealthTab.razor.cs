using Microsoft.AspNetCore.Components;
using MuuqWear.Model.DTO.AdminSystem;
using MuuqWear.Web.Services;

namespace MuuqWear.Web.Components.Pages.AdminComponent.SystemTech;

public partial class AdminSystemHealthTab
{
    [Inject] private AdminSystemTechIntegrationService IntegrationService { get; set; } = default!;

    private bool isLoading = true;
    private SystemHealthOverviewModel? overview;

    private string BackupDisplay =>
        string.IsNullOrWhiteSpace(overview?.LastBackupDisplay)
            ? "—"
            : overview.LastBackupDisplay;

    private string ActiveUsersDisplay =>
        overview?.ActiveUsersCount?.ToString() ?? "—";

    protected override async Task OnInitializedAsync()
    {
        await IntegrationService.RefreshOverviewAsync();
        overview = IntegrationService.Overview;
        isLoading = false;
    }

    private static string HealthLabel(bool healthy) =>
        healthy ? "✅ Connected" : "❌ Disconnected";
}
