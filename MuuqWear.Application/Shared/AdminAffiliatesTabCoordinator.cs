namespace MuuqWear.Application.Shared;

/// <summary>
/// Syncs Affiliates tab switches between the sidebar (layout island)
/// and AdminAffiliatesComponent (page island) without a full page reload.
/// </summary>
public class AdminAffiliatesTabCoordinator
{
    public event Action<string>? TabChanged;

    public void NotifyTabChanged(string tab)
    {
        TabChanged?.Invoke(tab);
    }

    public static string NormalizeTab(string? tab) =>
        tab?.ToLowerInvariant() switch
        {
            "active" => "active",
            "payouts" => "payouts",
            "tiers" => "tiers",
            _ => "pending"
        };
}
