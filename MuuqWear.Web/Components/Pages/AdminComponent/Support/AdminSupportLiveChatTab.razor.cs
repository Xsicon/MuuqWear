using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using MuuqWear.Application.Services.ChatService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Chat;
using MuuqWear.Web.Helpers;
using MuuqWear.Web.Services;

namespace MuuqWear.Web.Components.Pages.AdminComponent.Support;

public partial class AdminSupportLiveChatTab : IDisposable
{
    private const int CountUnchanged = -1;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MessageLoadTimeout = TimeSpan.FromSeconds(30);

    [Parameter] public EventCallback<(int chats, int tickets)> OnCountsChanged { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AdminSupportTabCoordinator SupportTabCoordinator { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private List<ChatSessionModel> sessions = [];
    private List<ChatMessageModel> messages = [];
    private readonly Dictionary<Guid, DateTime> readChatAtBySessionId = new();
    private AdminHeaderReadStateStore? readStateStore;
    private Guid? selectedSessionId;
    private ChatSessionModel? selectedSession;
    private string adminMessageInput = string.Empty;
    private bool isLoading = true;
    private bool isLoadingMessages;
    private bool messagesLoadSucceeded;
    private bool isSendingAdminMessage;
    private bool isClosingSession;
    private string? loadError;
    private string? messagesError;
    private string? actionError;
    private bool showKbPanel;
    private bool emailCopied;
    private SupportMacrosBar? macrosBar;
    private ElementReference messagesContainerRef;
    private bool scrollMessagesPending;
    private bool scrollMessagesForce;
    private CancellationTokenSource? pollCts;
    private CancellationTokenSource? messagesPollCts;
    private Task? sessionsPollTask;
    private Task? messagesPollTask;
    private int _messagesLoadVersion;
    private int _lastHeaderRefreshSignature = int.MinValue;
    private System.Threading.Timer? emailCopiedTimer;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            SupportTabCoordinator.SessionFocusRequested += OnSessionFocusRequested;
            SupportTabCoordinator.SessionReadRequested += OnSessionReadRequested;
            pollCts = new CancellationTokenSource();

            try
            {
                var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                var userId = AdminHeaderUserScope.GetUserId(authState.User);
                readStateStore = new AdminHeaderReadStateStore(JS, userId);
                foreach (var entry in await readStateStore.LoadChatReadAtAsync())
                    readChatAtBySessionId[entry.Key] = entry.Value;

                await LoadSessions();
                StartSessionsPolling();

                var focusSessionId = TryGetSessionIdFromUri();
                if (focusSessionId.HasValue && sessions.Any(s => s.Id == focusSessionId.Value))
                    await SelectSession(focusSessionId.Value);
                else if (sessions.Count > 0)
                    await SelectSession(sessions[0].Id);
            }
            catch (Exception ex)
            {
                loadError = AdminUiErrorHelper.FromException(ex);
            }
            finally
            {
                isLoading = false;
                await InvokeAsync(StateHasChanged);
            }

            await NotifyCounts();
        }

        if (!scrollMessagesPending)
            return;

        scrollMessagesPending = false;
        var force = scrollMessagesForce;
        scrollMessagesForce = false;
        await TryScrollMessagesAsync(force);
    }

    private void QueueScrollMessages(bool force = false)
    {
        scrollMessagesForce = scrollMessagesForce || force;
        scrollMessagesPending = true;
    }

    private async Task TryScrollMessagesAsync(bool force = false)
    {
        if (!selectedSessionId.HasValue)
            return;

        try
        {
            await JS.InvokeVoidAsync("chatScroll.scrollToBottom", messagesContainerRef, force);
        }
        catch (JSDisconnectedException)
        {
            // Circuit disconnected during scroll.
        }
        catch (InvalidOperationException)
        {
            // Element may not be rendered yet.
        }
    }

    private void OnSessionFocusRequested(Guid sessionId)
    {
        _ = InvokeAsync(async () =>
        {
            if (sessions.All(s => s.Id != sessionId))
                await LoadSessions();

            if (sessions.Any(s => s.Id == sessionId))
                await SelectSession(sessionId);
        });
    }

    private void OnSessionReadRequested(Guid sessionId, DateTime lastActivity)
    {
        MarkSessionReadLocally(sessionId, lastActivity);
        _ = InvokeAsync(StateHasChanged);
    }

    private void MarkSessionReadLocally(Guid sessionId, DateTime lastActivity)
    {
        AdminHeaderLiveChatMessagesBuilder.MarkSessionRead(
            readChatAtBySessionId,
            sessionId,
            lastActivity);
        _ = PersistReadStateAsync();
    }

    private Task PersistReadStateAsync() =>
        readStateStore?.SaveChatReadAtAsync(readChatAtBySessionId) ?? Task.CompletedTask;

    private int unreadChatCount =>
        AdminHeaderLiveChatMessagesBuilder.CountUnreadSessions(sessions, readChatAtBySessionId);

    private int GetSidebarMessageCount(ChatSessionModel session) =>
        AdminHeaderLiveChatMessagesBuilder.GetSidebarMessageCount(session, readChatAtBySessionId);

    private bool ShouldHighlightSidebarCount(ChatSessionModel session) =>
        AdminHeaderLiveChatMessagesBuilder.ShouldHighlightSidebarCount(session, readChatAtBySessionId);

    private Guid? TryGetSessionIdFromUri()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        if (!QueryHelpers.ParseQuery(uri.Query).TryGetValue("sessionId", out var value))
            return null;

        return Guid.TryParse(value.ToString(), out var sessionId) ? sessionId : null;
    }

    private async Task NotifyCounts()
    {
        if (OnCountsChanged.HasDelegate)
            await OnCountsChanged.InvokeAsync((unreadChatCount, CountUnchanged));
    }

    private int ComputeHeaderRefreshSignature()
    {
        var hash = new HashCode();
        hash.Add(unreadChatCount);
        foreach (var session in sessions.OrderBy(s => s.Id))
        {
            hash.Add(session.Id);
            hash.Add(session.LastActivity.Ticks);
            hash.Add(session.UnreadMessageCount);
            hash.Add(session.LastMessageSender);
            if (readChatAtBySessionId.TryGetValue(session.Id, out var readAt))
                hash.Add(readAt.Ticks);
        }

        return hash.ToHashCode();
    }

    private void RequestHeaderRefreshIfChanged()
    {
        var signature = ComputeHeaderRefreshSignature();
        if (signature == _lastHeaderRefreshSignature)
            return;

        _lastHeaderRefreshSignature = signature;
        SupportTabCoordinator.RequestMessagesRefresh();
        SupportTabCoordinator.RequestBadgeCountsRefresh();
    }

    private static bool IsWaiting(ChatSessionModel s) =>
        s.LastMessageSender is "customer" or null && s.Status == "active";

    private async Task LoadSessions()
    {
        try
        {
            var result = await ChatService.GetActiveSessions();

            if (result.Success && result.Data != null)
            {
                sessions = result.Data;
                loadError = null;

                if (selectedSessionId.HasValue)
                {
                    selectedSession = sessions.FirstOrDefault(s => s.Id == selectedSessionId);
                    if (selectedSession == null)
                    {
                        selectedSessionId = null;
                        messages = [];
                        messagesLoadSucceeded = false;
                        StopMessagesPolling();
                    }
                }

                await NotifyCounts();
                RequestHeaderRefreshIfChanged();
            }
            else
            {
                loadError = AdminUiErrorHelper.FromApi(result.Message, "Failed to load live chat sessions.");
            }
        }
        catch (Exception ex)
        {
            loadError = AdminUiErrorHelper.FromException(ex);
        }
    }

    private void StartSessionsPolling()
    {
        if (sessionsPollTask != null || pollCts == null)
            return;

        sessionsPollTask = PollSessionsAsync(pollCts.Token);
    }

    private async Task PollSessionsAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await InvokeAsync(async () =>
            {
                await LoadSessions();
                StateHasChanged();
            });
        }
    }

    private void StopSessionsPolling()
    {
        pollCts?.Cancel();
        pollCts?.Dispose();
        pollCts = null;
        sessionsPollTask = null;
        StopMessagesPolling();
    }

    private async Task LoadMessages()
    {
        if (!selectedSessionId.HasValue)
            return;

        var sessionId = selectedSessionId.Value;
        var loadVersion = _messagesLoadVersion;

        try
        {
            isLoadingMessages = messages.Count == 0;

            var loadTask = ChatService.GetMessages(sessionId);
            var completed = await Task.WhenAny(loadTask, Task.Delay(MessageLoadTimeout));

            if (completed != loadTask)
            {
                if (loadVersion == _messagesLoadVersion && selectedSessionId == sessionId)
                    messagesError = "Timed out loading messages. Please try again.";
                return;
            }

            var result = await loadTask;

            if (loadVersion != _messagesLoadVersion || selectedSessionId != sessionId)
                return;

            if (result.Success && result.Data != null)
            {
                ApplyServerMessages(result.Data);
                messagesLoadSucceeded = true;
                messagesError = null;
            }
            else
            {
                messagesLoadSucceeded = false;
                messagesError = AdminUiErrorHelper.FromApi(result.Message, "Failed to load messages.");
            }
        }
        catch (Exception ex)
        {
            if (loadVersion == _messagesLoadVersion && selectedSessionId == sessionId)
            {
                messagesLoadSucceeded = false;
                messagesError = AdminUiErrorHelper.FromException(ex);
            }
        }
        finally
        {
            if (loadVersion == _messagesLoadVersion && selectedSessionId == sessionId)
                isLoadingMessages = false;
        }
    }

    private void ApplyServerMessages(List<ChatMessageModel> serverMessages)
    {
        var serverIds = serverMessages.Select(m => m.Id).ToHashSet();
        var pendingLocal = messages.Where(m => !serverIds.Contains(m.Id)).ToList();
        var previousIds = messages.Select(m => m.Id).ToHashSet();

        if (isSendingAdminMessage && pendingLocal.Count > 0 && serverMessages.Count < messages.Count)
            return;

        var merged = serverMessages
            .Concat(pendingLocal)
            .OrderBy(m => m.CreatedAt)
            .ToList();

        if (merged.Count == messages.Count &&
            merged.Select(m => m.Id).SequenceEqual(messages.Select(m => m.Id)))
            return;

        messages = merged;

        if (merged.Any(m =>
                m.SenderType is "customer"
                && !previousIds.Contains(m.Id)))
        {
            SupportTabCoordinator.RequestMessagesRefresh();
            SupportTabCoordinator.RequestBadgeCountsRefresh();
        }

        QueueScrollMessages();
    }

    private void StartMessagesPolling()
    {
        messagesPollCts?.Cancel();
        messagesPollCts?.Dispose();

        if (pollCts == null)
            return;

        messagesPollCts = CancellationTokenSource.CreateLinkedTokenSource(pollCts.Token);
        messagesPollTask = PollMessagesAsync(messagesPollCts.Token);
    }

    private async Task PollMessagesAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await InvokeAsync(async () =>
            {
                await LoadMessages();
                StateHasChanged();
            });
        }
    }

    private void StopMessagesPolling()
    {
        messagesPollCts?.Cancel();
        messagesPollCts?.Dispose();
        messagesPollCts = null;
        messagesPollTask = null;
    }

    private async Task SelectSession(Guid sessionId)
    {
        _messagesLoadVersion++;
        selectedSessionId = sessionId;
        selectedSession = sessions.FirstOrDefault(s => s.Id == sessionId);
        emailCopied = false;
        messages = [];
        messagesLoadSucceeded = false;
        messagesError = null;
        actionError = null;
        isLoadingMessages = true;

        if (selectedSession != null && IsWaiting(selectedSession))
        {
            MarkSessionReadLocally(sessionId, selectedSession.LastActivity);
            SupportTabCoordinator.NotifySessionRead(sessionId, selectedSession.LastActivity);
        }

        StateHasChanged();

        await LoadSessionDetails(sessionId);
        await LoadMessages();
        StartMessagesPolling();
        RequestHeaderRefreshIfChanged();
        QueueScrollMessages(force: true);
        StateHasChanged();
    }

    private async Task LoadSessionDetails(Guid sessionId)
    {
        var result = await ChatService.GetSession(sessionId);
        if (!result.Success || result.Data == null)
        {
            actionError = AdminUiErrorHelper.FromApi(result.Message, "Failed to load conversation details.");
            return;
        }

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
            actionError = null;
            emailCopied = true;
            StateHasChanged();
            emailCopiedTimer?.Dispose();
            emailCopiedTimer = new System.Threading.Timer(_ =>
            {
                _ = InvokeAsync(() =>
                {
                    emailCopied = false;
                    StateHasChanged();
                });
            }, null, 2000, Timeout.Infinite);
        }
        catch (Exception ex)
        {
            actionError = AdminUiErrorHelper.FromException(ex);
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
            actionError = null;
            var result = await ChatService.SendMessage(new SendMessageRequest
            {
                SessionId = selectedSessionId.Value,
                Message = adminMessageInput
            });

            if (result.Success && result.Data != null)
            {
                messages.Add(result.Data);
                messagesLoadSucceeded = true;
                adminMessageInput = string.Empty;
                macrosBar?.Close();
                QueueScrollMessages(force: true);
            }
            else
            {
                actionError = AdminUiErrorHelper.FromApi(result.Message, "Failed to send message.");
            }
        }
        catch (Exception ex)
        {
            actionError = AdminUiErrorHelper.FromException(ex);
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

    private void OpenKbPanel() => showKbPanel = true;

    private void CloseKbPanel() => showKbPanel = false;

    private void ApplyMacro(string text) =>
        adminMessageInput = text;

    private async Task CloseSelectedSession()
    {
        if (!selectedSessionId.HasValue || isClosingSession) return;

        try
        {
            isClosingSession = true;
            actionError = null;
            var result = await ChatService.CloseSession(selectedSessionId.Value);

            if (result.Success)
            {
                sessions.RemoveAll(s => s.Id == selectedSessionId);
                selectedSessionId = null;
                selectedSession = null;
                messages = [];
                messagesLoadSucceeded = false;
                StopMessagesPolling();

                if (sessions.Count > 0)
                    await SelectSession(sessions[0].Id);

                await NotifyCounts();
                RequestHeaderRefreshIfChanged();
            }
            else
            {
                actionError = AdminUiErrorHelper.FromApi(result.Message, "Failed to close chat session.");
            }
        }
        catch (Exception ex)
        {
            actionError = AdminUiErrorHelper.FromException(ex);
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
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        return utc.ToLocalTime().ToString("MMM d");
    }

    public void Dispose()
    {
        SupportTabCoordinator.SessionFocusRequested -= OnSessionFocusRequested;
        SupportTabCoordinator.SessionReadRequested -= OnSessionReadRequested;
        emailCopiedTimer?.Dispose();
        StopSessionsPolling();
    }
}
