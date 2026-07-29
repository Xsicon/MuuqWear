using Microsoft.AspNetCore.Components;
using MuuqWear.Application.Services.HelpCenterService;
using MuuqWear.Model.HelpCenter;
using MuuqWear.Web.Services;

namespace MuuqWear.Web.Components.Pages.AdminComponent.Support;

public partial class AdminSupportTicketsTab : IDisposable
{
    private const int CountUnchanged = -1;

    [Parameter] public EventCallback<(int chats, int tickets)> OnCountsChanged { get; set; }

    private List<SupportTicketModel> tickets = [];
    private TicketStatsModel? stats;
    private bool isLoading = true;
    private string? loadError;
    private string search = string.Empty;
    private string filterStatus = "All";
    private string filterPriority = "All";
    private Guid? updatingId;
    private string? toast;
    private bool toastIsError;
    private bool showKbPanel;
    private System.Threading.Timer? toastTimer;

    private static readonly (string Key, string Label)[] StatusFilters =
    [
        ("All", "All"),
        ("open", "Open"),
        ("in_progress", "In Progress"),
        ("resolved", "Resolved")
    ];

    private static readonly string[] PriorityFilters = ["All", "high", "normal", "low"];

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
                 (filterPriority == "low" && t.Priority is not "high" and not "normal")));
        }
    }

    protected override async Task OnInitializedAsync()
    {
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
        var result = await HelpCenterService.GetAllTickets(null, 1, 100);
        if (result.Success && result.Data != null)
        {
            tickets = result.Data.Data;
            loadError = null;
        }
        else
        {
            tickets = [];
            loadError = AdminUiErrorHelper.FromApi(result.Message, "Failed to load support tickets.");
        }
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

    private async Task CycleStatus(SupportTicketModel ticket)
    {
        var next = ticket.Status switch
        {
            "open" => "in_progress",
            "in_progress" => "resolved",
            _ => "open"
        };

        updatingId = ticket.Id;
        StateHasChanged();

        try
        {
            var result = await HelpCenterService.UpdateTicketStatus(ticket.Id, next);
            if (result.Success)
            {
                ticket.Status = next;
                await LoadStats();
                await NotifyCounts();
                ShowToast("Ticket status updated");
            }
            else
            {
                ShowToast(AdminUiErrorHelper.FromApi(result.Message, "Failed to update ticket status."), isError: true);
            }
        }
        catch (Exception ex)
        {
            ShowToast(AdminUiErrorHelper.FromException(ex), isError: true);
        }
        finally
        {
            updatingId = null;
            StateHasChanged();
        }
    }

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

    private static string GetActionLabel(string status) => status switch
    {
        "open" => "Start",
        "in_progress" => "Resolve",
        _ => "Reopen"
    };

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
