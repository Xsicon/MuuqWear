namespace MuuqWear.Application.Shared;

/// <summary>
/// Syncs Customer Support tab switches between the sidebar and AdminCustomerSupportComponent.
/// </summary>
public class AdminSupportTabCoordinator
{
    public event Action<string>? TabChanged;
    public event Action? MessagesRefreshRequested;
    public event Action? BadgeCountsRefreshRequested;
    public event Action<Guid>? SessionFocusRequested;
    public event Action<Guid, DateTime>? SessionReadRequested;

    public void NotifyTabChanged(string tab) => TabChanged?.Invoke(tab);

    public void RequestMessagesRefresh() => MessagesRefreshRequested?.Invoke();

    public void RequestBadgeCountsRefresh() => BadgeCountsRefreshRequested?.Invoke();

    public void NotifySessionFocus(Guid sessionId) => SessionFocusRequested?.Invoke(sessionId);

    public void NotifySessionRead(Guid sessionId, DateTime lastActivity) =>
        SessionReadRequested?.Invoke(sessionId, lastActivity);

    public static string NormalizeTab(string? tab) =>
        tab?.ToLowerInvariant() switch
        {
            "tickets" => "tickets",
            "knowledge" => "knowledge",
            _ => "live-chat"
        };
}
