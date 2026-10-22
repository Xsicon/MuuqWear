using System.Security.Claims;
using MuuqWear.Application.Services.ChatService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Chat;

namespace MuuqWear.Web.Services;

public sealed class FirestoreChatService : IChatService
{
    private readonly AdminFirestore _store;
    private readonly IHttpContextAccessor _http;

    public FirestoreChatService(AdminFirestore store, IHttpContextAccessor http)
    {
        _store = store;
        _http = http;
    }

    public async Task<Response<ChatMessageModel>> SendMessage(SendMessageRequest request)
    {
        var user = _http.HttpContext?.User;
        var role = user?.FindFirst(ClaimTypes.Role)?.Value;
        var isStaff = !string.IsNullOrWhiteSpace(role) && role is not "user";
        var name = user?.FindFirst(ClaimTypes.Name)?.Value
                   ?? user?.FindFirst(ClaimTypes.Email)?.Value
                   ?? request.GuestName
                   ?? "Guest";
        return Response<ChatMessageModel>.SuccessResponse(
            await _store.SendChatMessageAsync(request, isStaff ? "admin" : "customer", name));
    }

    public async Task<Response<List<ChatMessageModel>>> GetMessages(Guid sessionId) =>
        Response<List<ChatMessageModel>>.SuccessResponse(await _store.ListChatMessagesAsync(sessionId));

    public async Task<Response<List<ChatSessionModel>>> GetActiveSessions() =>
        Response<List<ChatSessionModel>>.SuccessResponse(
            (await _store.ListChatSessionsAsync())
            .Where(s => !string.Equals(s.Status, "closed", StringComparison.OrdinalIgnoreCase))
            .ToList());

    public async Task<Response<ChatSessionModel>> GetSession(Guid sessionId)
    {
        var session = await _store.GetChatSessionAsync(sessionId);
        return session is null
            ? Response<ChatSessionModel>.Fail("Chat not found")
            : Response<ChatSessionModel>.SuccessResponse(session);
    }

    public async Task<Response<bool>> CloseSession(Guid sessionId)
    {
        await _store.CloseChatSessionAsync(sessionId);
        return Response<bool>.SuccessResponse(true);
    }

    public async Task<Response<string>> GetSessionStatus(Guid sessionId)
    {
        var session = await _store.GetChatSessionAsync(sessionId);
        return Response<string>.SuccessResponse(session?.Status ?? "closed");
    }
}
