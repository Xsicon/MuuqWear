using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using MuuqWear.Application.Services.ChatService;
using MuuqWear.Application.Services.HelpCenterService;
using MuuqWear.Model.Chat;
using MuuqWear.Model.HelpCenter;

namespace MuuqWear.Web.Components.Shared;

public partial class LiveChatWidgetComponent : IDisposable
{
    [CascadingParameter] private Task<AuthenticationState>? AuthStateTask { get; set; }

    [Inject] private IHelpCenterService HelpCenterService { get; set; } = default!;
    [Inject] private IChatService ChatService { get; set; } = default!;

    private bool isChatOpen;
    private string widgetState = "menu";

    private SubmitTicketModel ticketForm = new();
    private bool isSubmitting;
    private bool ticketSubmitted;
    private string ticketError = string.Empty;
    private string submittedTicketNumber = string.Empty;

    private Guid? chatSessionId;
    private List<ChatMessageModel> chatMessages = [];
    private string messageInput = string.Empty;
    private string? guestName;
    private string? guestEmail;
    private bool showGuestForm;
    private bool isSendingMessage;
    private bool isLoadingHistory;
    private string? chatErrorMessage;
    private bool isChatClosed;

    private System.Timers.Timer? pollTimer;
    private bool isPolling;

    /// <summary>Hard gate so Enter/click/re-entry cannot POST twice.</summary>
    private int _sendGate;

    private void OpenChatWidget()
    {
        isChatOpen = true;
        widgetState = "menu";
    }

    private void CloseChatWidget()
    {
        isChatOpen = false;
        StopPolling();
        ResetTicketForm();
    }

    private async Task HandleSubmitTicket()
    {
        ticketError = string.Empty;

        if (string.IsNullOrWhiteSpace(ticketForm.Name) ||
            string.IsNullOrWhiteSpace(ticketForm.Email) ||
            string.IsNullOrWhiteSpace(ticketForm.Category) ||
            string.IsNullOrWhiteSpace(ticketForm.Subject) ||
            string.IsNullOrWhiteSpace(ticketForm.Message))
        {
            ticketError = "Please fill in all fields.";
            return;
        }

        isSubmitting = true;
        StateHasChanged();

        var result = await HelpCenterService.SubmitTicket(ticketForm);

        if (result.Success)
        {
            ticketSubmitted = true;
            submittedTicketNumber = result.Data?.TicketNumber ?? string.Empty;
        }
        else
        {
            ticketError = result.Message ?? "Failed to submit ticket. Please try again.";
        }

        isSubmitting = false;
        StateHasChanged();
    }

    private void ResetTicketForm()
    {
        ticketForm = new SubmitTicketModel();
        ticketSubmitted = false;
        ticketError = string.Empty;
        submittedTicketNumber = string.Empty;
        widgetState = "menu";
    }

    private async Task HandleChatKeyDown(KeyboardEventArgs e)
    {
        if (e.Key != "Enter" || e.Repeat || e.ShiftKey)
            return;

        await SendMessage();
    }

    private async Task SendMessage()
    {
        if (Interlocked.CompareExchange(ref _sendGate, 1, 0) != 0)
            return;

        if (string.IsNullOrWhiteSpace(messageInput))
        {
            Interlocked.Exchange(ref _sendGate, 0);
            chatErrorMessage = "Message cannot be empty";
            return;
        }

        var text = messageInput.Trim();
        isSendingMessage = true;
        chatErrorMessage = null;
        messageInput = string.Empty;

        try
        {
            await InvokeAsync(StateHasChanged);

            var isAuthenticated = false;
            if (AuthStateTask != null)
            {
                var authState = await AuthStateTask;
                isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
            }

            if (!chatSessionId.HasValue && !isAuthenticated)
            {
                if (string.IsNullOrWhiteSpace(guestName))
                {
                    showGuestForm = true;
                    messageInput = text;
                    chatErrorMessage = "Please enter your name";
                    return;
                }

                if (string.IsNullOrWhiteSpace(guestEmail) || !guestEmail.Contains('@'))
                {
                    showGuestForm = true;
                    messageInput = text;
                    chatErrorMessage = "Please enter a valid email";
                    return;
                }
            }

            var request = new SendMessageRequest
            {
                SessionId = chatSessionId,
                Message = text,
                GuestName = isAuthenticated ? null : guestName,
                GuestEmail = isAuthenticated ? null : guestEmail
            };

            var result = await ChatService.SendMessage(request);

            if (result.Success && result.Data != null)
            {
                if (!chatSessionId.HasValue)
                {
                    chatSessionId = result.Data.SessionId;
                    StartPolling();
                }

                // Prefer server list so we never keep a local copy with a mismatched Id.
                await PollMessagesAsync();
                if (chatMessages.All(m => m.Id != result.Data.Id)
                    && chatMessages.All(m => !IsSameCustomerBubble(m, result.Data)))
                {
                    chatMessages.Add(result.Data);
                }

                showGuestForm = false;
            }
            else
            {
                messageInput = text;
                chatErrorMessage = result.Message ?? "Failed to send message";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LiveChatWidget] SendMessage error: {ex.Message}");
            messageInput = text;
            chatErrorMessage = "Failed to send message. Please try again.";
        }
        finally
        {
            isSendingMessage = false;
            Interlocked.Exchange(ref _sendGate, 0);
            await InvokeAsync(StateHasChanged);
        }
    }

    private static bool IsSameCustomerBubble(ChatMessageModel a, ChatMessageModel b) =>
        string.Equals(a.SenderType, b.SenderType, StringComparison.OrdinalIgnoreCase)
        && string.Equals(a.Message?.Trim(), b.Message?.Trim(), StringComparison.Ordinal)
        && Math.Abs((a.CreatedAt - b.CreatedAt).TotalSeconds) < 5;

    private async Task SubmitGuestInfo()
    {
        if (string.IsNullOrWhiteSpace(guestName))
        {
            chatErrorMessage = "Please enter your name";
            return;
        }

        if (string.IsNullOrWhiteSpace(guestEmail) || !guestEmail.Contains('@'))
        {
            chatErrorMessage = "Please enter a valid email";
            return;
        }

        chatErrorMessage = null;
        showGuestForm = false;
        await SendMessage();
    }

    private void StartNewChat()
    {
        StopPolling();
        chatSessionId = null;
        chatMessages.Clear();
        messageInput = string.Empty;
        guestName = null;
        guestEmail = null;
        isChatClosed = false;
        chatErrorMessage = null;
        showGuestForm = false;
        StateHasChanged();
    }

    private void StartPolling()
    {
        if (pollTimer != null) return;

        pollTimer = new System.Timers.Timer(3000);
        pollTimer.Elapsed += async (_, _) =>
        {
            await InvokeAsync(async () =>
            {
                await PollMessagesAsync();
                StateHasChanged();
            });
        };
        pollTimer.AutoReset = true;
        pollTimer.Start();
    }

    private void StopPolling()
    {
        pollTimer?.Stop();
        pollTimer?.Dispose();
        pollTimer = null;
    }

    private async Task PollMessagesAsync()
    {
        if (!chatSessionId.HasValue || isPolling) return;

        try
        {
            isPolling = true;

            var messagesTask = ChatService.GetMessages(chatSessionId.Value);
            var statusTask = ChatService.GetSessionStatus(chatSessionId.Value);
            await Task.WhenAll(messagesTask, statusTask);

            var messagesResult = await messagesTask;
            var statusResult = await statusTask;

            if (statusResult.Success && statusResult.Data == "closed" && !isChatClosed)
            {
                isChatClosed = true;
                StopPolling();
                return;
            }

            if (messagesResult.Success && messagesResult.Data != null)
                ApplyServerMessages(messagesResult.Data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LiveChatWidget] Poll error: {ex.Message}");
        }
        finally
        {
            isPolling = false;
        }
    }

    private void ApplyServerMessages(List<ChatMessageModel> serverMessages)
    {
        // Server is source of truth. Dedupe by Id, then by near-identical bubbles
        // (covers Id mismatch between send response and poll).
        var deduped = new List<ChatMessageModel>();
        foreach (var msg in serverMessages.OrderBy(m => m.CreatedAt))
        {
            if (deduped.Any(existing => existing.Id == msg.Id || IsSameCustomerBubble(existing, msg)))
                continue;
            deduped.Add(msg);
        }

        if (deduped.Count == chatMessages.Count &&
            deduped.Select(m => m.Id).SequenceEqual(chatMessages.Select(m => m.Id)))
            return;

        chatMessages = deduped;
    }

    public void Dispose() => StopPolling();
}
