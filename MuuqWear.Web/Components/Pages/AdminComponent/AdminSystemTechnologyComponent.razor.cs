using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using MuuqWear.Application.Shared;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminSystemTechnologyComponent : IDisposable
{
    private static readonly (string Id, string Label)[] TabViews =
    [
        ("health", "System Health"),
        ("integrations", "Integrations"),
        ("sync", "Sync Tools"),
        ("logs", "Logs"),
        ("jobs", "Background Jobs")
    ];

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AdminSystemTechTabCoordinator SystemTechTabCoordinator { get; set; } = default!;

    private string activeTab = "health";

    protected override void OnInitialized()
    {
        SystemTechTabCoordinator.TabChanged += OnSystemTechTabChanged;
        NavigationManager.LocationChanged += OnLocationChanged;
        ApplyTabFromUri();
    }

    private void OnSystemTechTabChanged(string tab)
    {
        var normalized = AdminSystemTechTabCoordinator.NormalizeTab(tab);
        if (activeTab == normalized)
            return;

        activeTab = normalized;
        InvokeAsync(StateHasChanged);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) =>
        InvokeAsync(() =>
        {
            ApplyTabFromUri();
            StateHasChanged();
        });

    private void ApplyTabFromUri()
    {
        var path = NavigationManager.ToBaseRelativePath(NavigationManager.Uri).Trim('/').ToLowerInvariant();
        if (!path.StartsWith("admin/system", StringComparison.Ordinal))
            return;

        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);
        activeTab = query.TryGetValue("tab", out var tab)
            ? AdminSystemTechTabCoordinator.NormalizeTab(tab)
            : "health";
    }

    private void SwitchTab(string tab)
    {
        var normalized = AdminSystemTechTabCoordinator.NormalizeTab(tab);
        if (activeTab == normalized)
            return;

        activeTab = normalized;
        NavigationManager.NavigateTo($"/admin/system?tab={normalized}");
        SystemTechTabCoordinator.NotifyTabChanged(normalized);
    }

    public void Dispose()
    {
        SystemTechTabCoordinator.TabChanged -= OnSystemTechTabChanged;
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
