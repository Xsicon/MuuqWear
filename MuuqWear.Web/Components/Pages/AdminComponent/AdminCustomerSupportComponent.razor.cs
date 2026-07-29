using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using MuuqWear.Application.Shared;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminCustomerSupportComponent : IDisposable
{
    private string activeTab = "live-chat";

    protected override void OnInitialized()
    {
        SupportTabCoordinator.TabChanged += OnSupportTabChanged;
        NavigationManager.LocationChanged += OnLocationChanged;
        ApplyTabFromUri();
    }

    private void OnSupportTabChanged(string tab)
    {
        var normalized = AdminSupportTabCoordinator.NormalizeTab(tab);
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

        if (path.StartsWith("admin/tickets", StringComparison.Ordinal))
        {
            activeTab = "tickets";
            return;
        }

        if (path.StartsWith("admin/live-chat", StringComparison.Ordinal))
        {
            activeTab = "live-chat";
            return;
        }

        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);
        activeTab = query.TryGetValue("tab", out var tab)
            ? AdminSupportTabCoordinator.NormalizeTab(tab)
            : "live-chat";
    }

    public void Dispose()
    {
        SupportTabCoordinator.TabChanged -= OnSupportTabChanged;
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
