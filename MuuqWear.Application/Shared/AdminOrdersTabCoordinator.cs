namespace MuuqWear.Application.Shared;

/// <summary>
/// Syncs Sales & Orders tab switches between the sidebar (layout island)
/// and AdminOrdersComponent (page island) without a full page reload.
/// </summary>
public class AdminOrdersTabCoordinator
{
    public event Action<string>? TabChanged;
    public event Action? BadgeCountsRefreshRequested;

    public void NotifyTabChanged(string tab)
    {
        TabChanged?.Invoke(tab);
    }

    public void RequestBadgeCountsRefresh()
    {
        BadgeCountsRefreshRequested?.Invoke();
    }

    public static string NormalizeTab(string? tab) =>
        tab?.ToLowerInvariant() switch
        {
            "returns" => "returns",
            "refunds" => "refunds",
            _ => "orders"
        };
}
