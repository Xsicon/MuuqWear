using MuuqWear.Application.Shared;
using MuuqWear.Model.Chat;

namespace MuuqWear.Application.Services.ChatService;

public interface IChatService
{
    Task<Response<ChatMessageModel>> SendMessage(SendMessageRequest request);
    Task<Response<List<ChatMessageModel>>> GetMessages(Guid sessionId);
    Task<Response<List<ChatSessionModel>>> GetActiveSessions();
    Task<Response<bool>> CloseSession(Guid sessionId);
    Task<Response<string>> GetSessionStatus(Guid sessionId);
}
