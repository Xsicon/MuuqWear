namespace MuuqWear.Application.Shared;

/// <summary>
/// Syncs Customer Support tab switches between the sidebar and AdminCustomerSupportComponent.
/// </summary>
public class AdminSupportTabCoordinator
{
    public event Action<string>? TabChanged;

    public void NotifyTabChanged(string tab) => TabChanged?.Invoke(tab);

    public static string NormalizeTab(string? tab) =>
        tab?.ToLowerInvariant() switch
        {
            "tickets" => "tickets",
            "knowledge" => "knowledge",
            _ => "live-chat"
        };
}
