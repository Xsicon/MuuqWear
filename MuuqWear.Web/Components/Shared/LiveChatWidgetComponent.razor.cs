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

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(messageInput))
        {
            chatErrorMessage = "Message cannot be empty";
            return;
        }

        try
        {
            isSendingMessage = true;
            chatErrorMessage = null;
            StateHasChanged();

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
                    chatErrorMessage = "Please enter your name";
                    return;
                }

                if (string.IsNullOrWhiteSpace(guestEmail) || !guestEmail.Contains('@'))
                {
                    showGuestForm = true;
                    chatErrorMessage = "Please enter a valid email";
                    return;
                }
            }

            var request = new SendMessageRequest
            {
                SessionId = chatSessionId,
                Message = messageInput,
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

                chatMessages.Add(result.Data);
                messageInput = string.Empty;
                showGuestForm = false;
            }
            else
            {
                chatErrorMessage = result.Message ?? "Failed to send message";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LiveChatWidget] SendMessage error: {ex.Message}");
            chatErrorMessage = "Failed to send message. Please try again.";
        }
        finally
        {
            isSendingMessage = false;
            StateHasChanged();
        }
    }

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

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !isSendingMessage && !string.IsNullOrWhiteSpace(messageInput))
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
        if (isSendingMessage && serverMessages.Count < chatMessages.Count)
            return;

        var serverIds = serverMessages.Select(m => m.Id).ToHashSet();
        var pendingLocal = chatMessages.Where(m => !serverIds.Contains(m.Id)).ToList();
        var merged = serverMessages
            .Concat(pendingLocal)
            .OrderBy(m => m.CreatedAt)
            .ToList();

        if (merged.Count == chatMessages.Count &&
            merged.Select(m => m.Id).SequenceEqual(chatMessages.Select(m => m.Id)))
            return;

        chatMessages = merged;
    }

    public void Dispose() => StopPolling();
}
