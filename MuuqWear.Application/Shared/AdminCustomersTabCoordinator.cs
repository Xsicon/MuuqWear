namespace MuuqWear.Application.Shared;

/// <summary>
/// Syncs Customers view switches between the sidebar (layout island)
/// and AdminCustomerComponent (page island) without a full page reload.
/// </summary>
public class AdminCustomersTabCoordinator
{
    public event Action<string>? ViewChanged;
    public event Action? MessagesRefreshRequested;
    public event Action<Guid>? CustomerFocusRequested;

    public void NotifyViewChanged(string view)
    {
        ViewChanged?.Invoke(view);
    }

    public void RequestMessagesRefresh()
    {
        MessagesRefreshRequested?.Invoke();
    }

    public void NotifyCustomerFocus(Guid customerId)
    {
        CustomerFocusRequested?.Invoke(customerId);
    }

    public static string NormalizeView(string? view) =>
        view?.ToLowerInvariant() switch
        {
            "details" => "details",
            "notes" => "notes",
            _ => "list"
        };
}
