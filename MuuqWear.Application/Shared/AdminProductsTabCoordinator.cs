namespace MuuqWear.Application.Shared;

/// <summary>
/// Syncs Products & Inventory view switches between the sidebar (layout island)
/// and AdminProductComponent (page island) without a full page reload.
/// </summary>
public class AdminProductsTabCoordinator
{
    public event Action<string>? ViewChanged;
    public event Action? BadgeCountsRefreshRequested;
    public event Action<Guid>? ProductFocusRequested;

    public void NotifyViewChanged(string view)
    {
        ViewChanged?.Invoke(view);
    }

    public void RequestBadgeCountsRefresh()
    {
        BadgeCountsRefreshRequested?.Invoke();
    }

    public void NotifyProductFocus(Guid productId)
    {
        ProductFocusRequested?.Invoke(productId);
    }

    public static string NormalizeView(string? view) =>
        view?.ToLowerInvariant() switch
        {
            "stock" => "stock",
            "low-stock" => "low-stock",
            "restock" => "restock",
            _ => "catalog"
        };
}

