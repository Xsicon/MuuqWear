using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MuuqWear.Application.Services.HelpCenterService;
using MuuqWear.Model.HelpCenter;
using MuuqWear.Web.Helpers;
using MuuqWear.Web.Services;

namespace MuuqWear.Web.Components.Pages.AdminComponent.Support;

public partial class AdminSupportTicketsTab : IDisposable
{
    private const int CountUnchanged = -1;

    [Parameter] public EventCallback<(int chats, int tickets)> OnCountsChanged { get; set; }

    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private List<SupportTicketModel> tickets = [];
    private TicketStatsModel? stats;
    private SupportTicketModel? activeTicket;
    private bool isLoading = true;
    private bool drawerLoading;
    private string? loadError;
    private string? listWarning;
    private string search = string.Empty;
    private string filterStatus = "All";
    private string filterPriority = "All";
    private bool mineOnly;
    private string agentName = string.Empty;
    private string? toast;
    private bool toastIsError;
    private bool showKbPanel;
    private System.Threading.Timer? toastTimer;

    private string ShellClass =>
        showKbPanel ? "cs-tab-shell cs-tab-shell--kb-open" : "cs-tab-shell";

    private static readonly (string Key, string Label)[] StatusFilters =
    [
        ("All", "All"),
        ("open", "Open"),
        ("in_progress", "In Progress"),
        ("resolved", "Resolved")
    ];

    private static readonly string[] PriorityFilters = ["All", "high", "normal", "low"];

    private int MineCount =>
        string.IsNullOrWhiteSpace(agentName)
            ? 0
            : tickets.Count(t => t.IsAssignedTo(agentName));

    private IEnumerable<SupportTicketModel> FilteredTickets
    {
        get
        {
            var q = search.Trim().ToLowerInvariant();
            return tickets.Where(t =>
                (string.IsNullOrEmpty(q) ||
                 t.Subject.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                 t.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                 t.Email.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                 t.TicketNumber.Contains(q, StringComparison.OrdinalIgnoreCase)) &&
                (filterStatus == "All" || t.Status == filterStatus) &&
                (filterPriority == "All" || t.Priority == filterPriority ||
                 (filterPriority == "low" && t.Priority is not "high" and not "normal")) &&
                (!mineOnly || t.IsAssignedTo(agentName)));
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        (agentName, _, _) = AdminPortalUserContext.FromClaims(auth.User);

        try
        {
            await Task.WhenAll(LoadTickets(), LoadStats());
        }
        catch (Exception ex)
        {
            loadError = AdminUiErrorHelper.FromException(ex);
        }
        finally
        {
            isLoading = false;
            await NotifyCounts();
        }
    }

    private async Task LoadTickets()
    {
        var (items, error, truncation) = await SupportPaginatedLoader.LoadAllPagesAsync(
            (page, pageSize) => HelpCenterService.GetAllTickets(null, page, pageSize));

        if (error != null && items.Count == 0)
        {
            tickets = [];
            listWarning = null;
            loadError = AdminUiErrorHelper.FromApi(error, "Failed to load support tickets.");
            return;
        }

        tickets = items;
        listWarning = truncation;
        loadError = null;
    }

    private async Task LoadStats()
    {
        var result = await HelpCenterService.GetStats();
        if (result.Success && result.Data != null)
            stats = result.Data;
    }

    private async Task NotifyCounts()
    {
        if (OnCountsChanged.HasDelegate)
        {
            var open = stats?.OpenCount ?? tickets.Count(t => t.Status == "open");
            await OnCountsChanged.InvokeAsync((CountUnchanged, open));
        }
    }

    private async Task OpenTicketDrawerAsync(SupportTicketModel ticket)
    {
        activeTicket = ticket;
        drawerLoading = true;
        StateHasChanged();

        try
        {
            var result = await HelpCenterService.GetTicketById(ticket.Id);
            if (result.Success && result.Data != null)
            {
                activeTicket = result.Data;
                SyncTicketInList(result.Data);
            }
            else
            {
                ShowToast(
                    AdminUiErrorHelper.FromApi(result.Message, "Failed to load ticket details."),
                    isError: true);
            }
        }
        catch (Exception ex)
        {
            ShowToast(AdminUiErrorHelper.FromException(ex), isError: true);
        }
        finally
        {
            drawerLoading = false;
            StateHasChanged();
        }
    }

    private void CloseTicketDrawer() => activeTicket = null;

    private Task HandleTicketUpdated(SupportTicketModel updated)
    {
        SyncTicketInList(updated);
        activeTicket = updated;
        return Task.CompletedTask;
    }

    private Task HandleDrawerNotify((string Message, bool IsError) notification)
    {
        ShowToast(notification.Message, notification.IsError);
        return Task.CompletedTask;
    }

    private void SyncTicketInList(SupportTicketModel updated)
    {
        var index = tickets.FindIndex(t => t.Id == updated.Id);
        if (index >= 0)
            tickets[index] = updated;
    }

    private void ToggleMineOnly() => mineOnly = !mineOnly;

    private void ShowToast(string message, bool isError = false)
    {
        toast = message;
        toastIsError = isError;
        toastTimer?.Dispose();
        toastTimer = new System.Threading.Timer(_ =>
        {
            _ = InvokeAsync(() =>
            {
                toast = null;
                toastIsError = false;
                StateHasChanged();
            });
        }, null, 2400, Timeout.Infinite);
    }

    private static string GetStatusLabel(string status) => status switch
    {
        "in_progress" => "In Progress",
        "resolved" => "Resolved",
        _ => "Open"
    };

    private static string GetPriorityLabel(string priority) => priority switch
    {
        "high" => "HIGH",
        "normal" => "MEDIUM",
        _ => "LOW"
    };

    private static (string Bg, string Text, string Dot) GetStatusStyle(string status) => status switch
    {
        "in_progress" => ("#FEF3C7", "#92400E", "#F59E0B"),
        "resolved" => ("#D1FAE5", "#065F46", "#22C55E"),
        _ => ("#DBEAFE", "#1E3A8A", "#3B82F6")
    };

    private static (string Bg, string Text) GetPriorityStyle(string priority) => priority switch
    {
        "high" => ("#FEE2E2", "#991B1B"),
        "normal" => ("#FEF3C7", "#92400E"),
        _ => ("#F3F4F6", "#374151")
    };

    private static string FormatDate(DateTime? dt) =>
        dt?.ToString("MMM d, yyyy") ?? "—";

    private void OpenKbPanel() => showKbPanel = true;

    private void CloseKbPanel() => showKbPanel = false;

    public void Dispose() => toastTimer?.Dispose();
}
