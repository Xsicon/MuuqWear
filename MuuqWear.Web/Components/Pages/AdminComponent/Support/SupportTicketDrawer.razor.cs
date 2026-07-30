using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MuuqWear.Application.Services.HelpCenterService;
using MuuqWear.Model.HelpCenter;
using MuuqWear.Web.Helpers;
using MuuqWear.Web.Services;

namespace MuuqWear.Web.Components.Pages.AdminComponent.Support;

public partial class SupportTicketDrawer
{
    [Parameter, EditorRequired] public SupportTicketModel Ticket { get; set; } = default!;
    [Parameter] public bool Shifted { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnOpenKb { get; set; }
    [Parameter] public EventCallback<(string Message, bool IsError)> OnNotify { get; set; }
    [Parameter] public EventCallback<SupportTicketModel> OnTicketUpdated { get; set; }

    [Inject] private IHelpCenterService HelpCenterService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private SupportMacrosBar? macrosBar;
    private string replyText = string.Empty;
    private string agentName = "Agent";
    private bool isSaving;
    private bool scrollAfterRender;
    private bool wasLoading = true;

    private bool IsSaving
    {
        get => isSaving;
        set
        {
            isSaving = value;
            StateHasChanged();
        }
    }

    private IReadOnlyList<string> AgentOptions =>
        SupportTicketAgents.WithCurrentUser(agentName);

    private string DisplayTeam =>
        string.IsNullOrWhiteSpace(Ticket.Team) || Ticket.Team == SupportTicketTeams.Unassigned
            ? SupportTicketTeams.Unassigned
            : Ticket.Team;

    private string DisplayAgent =>
        string.IsNullOrWhiteSpace(Ticket.AssignedToName) ||
        Ticket.AssignedToName == SupportTicketAgents.Unassigned
            ? SupportTicketAgents.Unassigned
            : Ticket.AssignedToName;

    private bool IsAssignedToMe => Ticket.IsAssignedTo(agentName);

    private IEnumerable<SupportTicketReplyModel> VisibleReplies =>
        Ticket.Replies.Where(r => !IsDuplicateOriginalMessage(r));

    private bool IsDuplicateOriginalMessage(SupportTicketReplyModel reply) =>
        !reply.IsAgent &&
        string.Equals(reply.Message.Trim(), Ticket.Message.Trim(), StringComparison.Ordinal);

    private string CustomerInitials
    {
        get
        {
            var parts = Ticket.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
            return parts.Length > 0
                ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
                : "?";
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        var (name, _, _) = AdminPortalUserContext.FromClaims(auth.User);
        agentName = name;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (wasLoading && !IsLoading)
            scrollAfterRender = true;

        wasLoading = IsLoading;
        await Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!scrollAfterRender)
            return;

        scrollAfterRender = false;
        if (Ticket.Replies.Count > 0)
            await ScrollThreadToEndAsync();
    }

    private async Task ScrollThreadToEndAsync()
    {
        try
        {
            await JS.InvokeVoidAsync(
                "eval",
                "(function(){const el=document.querySelector('.cs-ticket-thread');if(el)el.scrollTop=el.scrollHeight;})();");
        }
        catch
        {
            // ignore scroll errors during prerender/disconnect
        }
    }

    private async Task AssignToMeAsync()
    {
        IsSaving = true;
        try
        {
            var result = await HelpCenterService.AssignTicketToMe(Ticket.Id);
            if (result.Success && result.Data != null)
            {
                await ApplyTicketUpdate(result.Data);
                await NotifyAsync("Assigned to you");
            }
            else
            {
                var fallback = await HelpCenterService.UpdateTicket(Ticket.Id, new UpdateTicketModel
                {
                    AssignedToName = agentName,
                    Status = Ticket.Status == "open" ? "in_progress" : Ticket.Status
                });
                if (fallback.Success && fallback.Data != null)
                {
                    await ApplyTicketUpdate(fallback.Data);
                    await NotifyAsync("Assigned to you");
                }
                else
                {
                    await NotifyAsync(
                        AdminUiErrorHelper.FromApi(result.Message, "Failed to assign ticket."),
                        isError: true);
                }
            }
        }
        catch (Exception ex)
        {
            await NotifyAsync(AdminUiErrorHelper.FromException(ex), isError: true);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task OnStatusChanged(ChangeEventArgs e)
    {
        var next = e.Value?.ToString();
        if (string.IsNullOrWhiteSpace(next) || next == Ticket.Status)
            return;

        await PatchTicketAsync(new UpdateTicketModel { Status = next }, "Status updated");
    }

    private async Task OnPriorityChanged(ChangeEventArgs e)
    {
        var next = e.Value?.ToString();
        if (string.IsNullOrWhiteSpace(next) || next == Ticket.Priority)
            return;

        await PatchTicketAsync(new UpdateTicketModel { Priority = next }, "Priority updated");
    }

    private async Task OnTeamChanged(ChangeEventArgs e)
    {
        var next = e.Value?.ToString() ?? SupportTicketTeams.Unassigned;
        var team = next == SupportTicketTeams.Unassigned ? string.Empty : next;
        if ((Ticket.Team ?? string.Empty) == team)
            return;

        await PatchTicketAsync(new UpdateTicketModel { Team = team }, "Team assigned");
    }

    private async Task OnAgentChanged(ChangeEventArgs e)
    {
        var next = e.Value?.ToString() ?? SupportTicketAgents.Unassigned;
        var assigned = next == SupportTicketAgents.Unassigned ? string.Empty : next;
        if ((Ticket.AssignedToName ?? string.Empty) == assigned)
            return;

        await PatchTicketAsync(new UpdateTicketModel
        {
            AssignedToName = assigned,
            AssignedTo = null
        }, "Agent assigned");
    }

    private async Task PatchTicketAsync(UpdateTicketModel request, string successMessage)
    {
        IsSaving = true;
        try
        {
            var result = await HelpCenterService.UpdateTicket(Ticket.Id, request);
            if (result.Success && result.Data != null)
            {
                await ApplyTicketUpdate(result.Data);
                await NotifyAsync(successMessage);
            }
            else
            {
                await NotifyAsync(
                    AdminUiErrorHelper.FromApi(result.Message, "Failed to update ticket."),
                    isError: true);
            }
        }
        catch (Exception ex)
        {
            await NotifyAsync(AdminUiErrorHelper.FromException(ex), isError: true);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task SendReplyAsync()
    {
        if (string.IsNullOrWhiteSpace(replyText))
            return;

        var message = replyText.Trim();
        IsSaving = true;
        try
        {
            var result = await HelpCenterService.AddTicketReply(Ticket.Id, message);
            if (result.Success && result.Data != null)
            {
                replyText = string.Empty;
                macrosBar?.Close();

                var refreshed = await HelpCenterService.GetTicketById(Ticket.Id);
                if (refreshed.Success && refreshed.Data != null)
                    await ApplyTicketUpdate(refreshed.Data);
                else
                {
                    Ticket.Replies.Add(result.Data);
                    if (Ticket.FirstResponseAt == null && result.Data.IsAgent)
                        Ticket.FirstResponseAt = result.Data.CreatedAt ?? DateTime.UtcNow;
                    if (Ticket.Status == "open")
                        Ticket.Status = "in_progress";
                    await ApplyTicketUpdate(Ticket);
                }

                await NotifyAsync("Reply sent to customer");
                await ScrollThreadToEndAsync();
            }
            else
            {
                await NotifyAsync(
                    AdminUiErrorHelper.FromApi(result.Message, "Failed to send reply."),
                    isError: true);
            }
        }
        catch (Exception ex)
        {
            await NotifyAsync(AdminUiErrorHelper.FromException(ex), isError: true);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task ApplyMacro(string text)
    {
        replyText = string.IsNullOrWhiteSpace(replyText)
            ? text
            : $"{replyText.Trim()} {text}";
        await Task.CompletedTask;
    }

    private async Task HandleReplyKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
            await SendReplyAsync();
    }

    private async Task ApplyTicketUpdate(SupportTicketModel updated)
    {
        Ticket = updated;
        await OnTicketUpdated.InvokeAsync(updated);
        StateHasChanged();
    }

    private static string FormatTimeAgo(DateTime? dt)
    {
        if (dt == null)
            return "—";

        var mins = Math.Max(0, (int)(DateTime.UtcNow - dt.Value.ToUniversalTime()).TotalMinutes);
        if (mins < 60) return $"{mins}m ago";
        var hrs = mins / 60;
        if (hrs < 24) return $"{hrs}h ago";
        var days = hrs / 24;
        if (days < 30) return $"{days}d ago";
        return dt.Value.ToString("MMM d, yyyy");
    }

    private static string FormatReplyTime(DateTime? dt) =>
        dt?.ToString("MMM d, yyyy · h:mm tt") ?? "—";

    private async Task NotifyAsync(string message, bool isError = false)
    {
        if (OnNotify.HasDelegate)
            await OnNotify.InvokeAsync((message, isError));
    }
}
