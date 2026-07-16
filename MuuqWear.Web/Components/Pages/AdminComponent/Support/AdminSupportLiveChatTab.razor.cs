using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MuuqWear.Application.Services.ChatService;
using MuuqWear.Model.Chat;

namespace MuuqWear.Web.Components.Pages.AdminComponent.Support;

public partial class AdminSupportLiveChatTab : IDisposable
{
    private const int CountUnchanged = -1;

    [Parameter] public EventCallback<(int chats, int tickets)> OnCountsChanged { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private List<ChatSessionModel> sessions = [];
    private List<ChatMessageModel> messages = [];
    private Guid? selectedSessionId;
    private ChatSessionModel? selectedSession;
    private string adminMessageInput = string.Empty;
    private bool isLoading = true;
    private bool isLoadingMessages;
    private bool isSendingAdminMessage;
    private bool isClosingSession;
    private bool isPollingSessions;
    private bool isPollingMessages;
    private System.Timers.Timer? sessionsTimer;
    private System.Timers.Timer? messagesTimer;
    private int resolvedTodayCount;
    private DateOnly resolvedTodayDate = DateOnly.FromDateTime(DateTime.Now);

    private int activeCount => sessions.Count(s => !IsWaiting(s));
    private int waitingCount => sessions.Count(IsWaiting);
    private int resolvedToday
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            if (today != resolvedTodayDate)
            {
                resolvedTodayDate = today;
                resolvedTodayCount = 0;
            }

            return resolvedTodayCount;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadSessions();
        StartSessionsPolling();
        isLoading = false;

        if (sessions.Count > 0)
            await SelectSession(sessions[0].Id);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await NotifyCounts();
    }

    private async Task NotifyCounts()
    {
        if (OnCountsChanged.HasDelegate)
            await OnCountsChanged.InvokeAsync((sessions.Count, CountUnchanged));
    }

    private static bool IsWaiting(ChatSessionModel s) =>
        s.LastMessageSender is "customer" or null && s.Status == "active";

    private async Task LoadSessions()
    {
        if (isPollingSessions) return;

        try
        {
            isPollingSessions = true;
            var result = await ChatService.GetActiveSessions();

            if (result.Success && result.Data != null)
            {
                sessions = result.Data;

                if (selectedSessionId.HasValue)
                {
                    selectedSession = sessions.FirstOrDefault(s => s.Id == selectedSessionId);
                    if (selectedSession == null)
                    {
                        selectedSessionId = null;
                        messages.Clear();
                        StopMessagesPolling();
                    }
                }

                await NotifyCounts();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminSupportLiveChat] LoadSessions: {ex.Message}");
        }
        finally
        {
            isPollingSessions = false;
        }
    }

    private void StartSessionsPolling()
    {
        if (sessionsTimer != null) return;

        sessionsTimer = new System.Timers.Timer(3000);
        sessionsTimer.Elapsed += async (_, _) =>
        {
            await InvokeAsync(async () =>
            {
                await LoadSessions();
                StateHasChanged();
            });
        };
        sessionsTimer.AutoReset = true;
        sessionsTimer.Start();
    }

    private void StopSessionsPolling()
    {
        sessionsTimer?.Stop();
        sessionsTimer?.Dispose();
        sessionsTimer = null;
    }

    private async Task LoadMessages()
    {
        if (!selectedSessionId.HasValue || isPollingMessages) return;

        try
        {
            isPollingMessages = true;
            isLoadingMessages = messages.Count == 0;

            var result = await ChatService.GetMessages(selectedSessionId.Value);
            if (result.Success && result.Data != null)
                ApplyServerMessages(result.Data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminSupportLiveChat] LoadMessages: {ex.Message}");
        }
        finally
        {
            isPollingMessages = false;
            isLoadingMessages = false;
        }
    }

    private void ApplyServerMessages(List<ChatMessageModel> serverMessages)
    {
        if (isSendingAdminMessage && serverMessages.Count < messages.Count)
            return;

        var serverIds = serverMessages.Select(m => m.Id).ToHashSet();
        var pendingLocal = messages.Where(m => !serverIds.Contains(m.Id)).ToList();
        var merged = serverMessages
            .Concat(pendingLocal)
            .OrderBy(m => m.CreatedAt)
            .ToList();

        if (merged.Count == messages.Count &&
            merged.Select(m => m.Id).SequenceEqual(messages.Select(m => m.Id)))
            return;

        messages = merged;
    }

    private void StartMessagesPolling()
    {
        if (messagesTimer != null) return;

        messagesTimer = new System.Timers.Timer(3000);
        messagesTimer.Elapsed += async (_, _) =>
        {
            await InvokeAsync(async () =>
            {
                await LoadMessages();
                StateHasChanged();
            });
        };
        messagesTimer.AutoReset = true;
        messagesTimer.Start();
    }

    private void StopMessagesPolling()
    {
        messagesTimer?.Stop();
        messagesTimer?.Dispose();
        messagesTimer = null;
    }

    private async Task SelectSession(Guid sessionId)
    {
        StopMessagesPolling();
        selectedSessionId = sessionId;
        selectedSession = sessions.FirstOrDefault(s => s.Id == sessionId);
        messages.Clear();
        await Task.WhenAll(LoadSessionDetails(sessionId), LoadMessages());
        StartMessagesPolling();
    }

    private async Task LoadSessionDetails(Guid sessionId)
    {
        var result = await ChatService.GetSession(sessionId);
        if (!result.Success || result.Data == null)
            return;

        var details = result.Data;
        var index = sessions.FindIndex(s => s.Id == sessionId);
        if (index >= 0)
        {
            sessions[index].CustomerName = string.IsNullOrWhiteSpace(details.CustomerName)
                ? sessions[index].CustomerName
                : details.CustomerName;
            sessions[index].CustomerEmail ??= details.CustomerEmail;
            sessions[index].Email ??= details.Email;
            sessions[index].GuestEmail ??= details.GuestEmail;
            sessions[index].Status = details.Status;
            sessions[index].LastActivity = details.LastActivity;
        }

        if (selectedSessionId == sessionId)
            selectedSession = index >= 0 ? sessions[index] : details;
    }

    private async Task CopyCustomerEmail()
    {
        var email = selectedSession?.ContactEmail;
        if (string.IsNullOrWhiteSpace(email))
            return;

        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", email);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminSupportLiveChat] Copy email: {ex.Message}");
        }
    }

    private async Task SendAdminMessage()
    {
        if (!selectedSessionId.HasValue ||
            string.IsNullOrWhiteSpace(adminMessageInput) ||
            isSendingAdminMessage)
            return;

        try
        {
            isSendingAdminMessage = true;
            var result = await ChatService.SendMessage(new SendMessageRequest
            {
                SessionId = selectedSessionId.Value,
                Message = adminMessageInput
            });

            if (result.Success && result.Data != null)
            {
                messages.Add(result.Data);
                adminMessageInput = string.Empty;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminSupportLiveChat] Send: {ex.Message}");
        }
        finally
        {
            isSendingAdminMessage = false;
        }
    }

    private async Task HandleAdminKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !isSendingAdminMessage && !string.IsNullOrWhiteSpace(adminMessageInput))
            await SendAdminMessage();
    }

    private async Task CloseSelectedSession()
    {
        if (!selectedSessionId.HasValue || isClosingSession) return;

        try
        {
            isClosingSession = true;
            var result = await ChatService.CloseSession(selectedSessionId.Value);

            if (result.Success)
            {
                _ = resolvedToday;
                resolvedTodayCount++;

                sessions.RemoveAll(s => s.Id == selectedSessionId);
                selectedSessionId = null;
                selectedSession = null;
                messages.Clear();
                StopMessagesPolling();

                if (sessions.Count > 0)
                    await SelectSession(sessions[0].Id);

                await NotifyCounts();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminSupportLiveChat] Close: {ex.Message}");
        }
        finally
        {
            isClosingSession = false;
        }
    }

    private static string GetRelativeTime(DateTime utc)
    {
        var diff = DateTime.UtcNow - utc;
        if (diff.TotalSeconds < 60) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} h";
        return utc.ToLocalTime().ToString("MMM d");
    }

    public void Dispose()
    {
        StopSessionsPolling();
        StopMessagesPolling();
    }
}
