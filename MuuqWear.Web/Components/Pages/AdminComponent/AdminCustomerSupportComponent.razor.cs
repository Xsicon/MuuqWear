using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using MuuqWear.Application.Shared;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminCustomerSupportComponent : IDisposable
{
    private static readonly (string Id, string Label)[] TabViews =
    [
        ("live-chat", "Live Chat"),
        ("tickets", "Support Tickets"),
        ("knowledge", "Knowledge Base")
    ];

    private string activeTab = "live-chat";
    private int activeChats;
    private int openTickets;

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

    private void SwitchTab(string tab)
    {
        var normalized = AdminSupportTabCoordinator.NormalizeTab(tab);
        if (activeTab == normalized)
            return;

        activeTab = normalized;
        NavigationManager.NavigateTo($"/admin/support?tab={normalized}");
        SupportTabCoordinator.NotifyTabChanged(normalized);
    }

    private Task HandleCountsChanged((int chats, int tickets) counts)
    {
        // Tabs pass -1 for the sibling metric so switching tabs does not zero the other badge.
        if (counts.chats >= 0)
            activeChats = counts.chats;
        if (counts.tickets >= 0)
            openTickets = counts.tickets;
        return InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        SupportTabCoordinator.TabChanged -= OnSupportTabChanged;
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
