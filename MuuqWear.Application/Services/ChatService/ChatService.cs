using MuuqWear.Application.Shared;
using MuuqWear.Model.Chat;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.ChatService;

public class ChatService : IChatService
{
    private readonly HttpClient _httpClient;

    public ChatService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Response<ChatMessageModel>> SendMessage(SendMessageRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/Chat/send", request);
            return await HttpResponseReader.ReadAsync<ChatMessageModel>(response);
        }
        catch (Exception ex)
        {
            return Response<ChatMessageModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    public async Task<Response<List<ChatMessageModel>>> GetMessages(Guid sessionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/Chat/messages/{sessionId}");
            return await HttpResponseReader.ReadAsync<List<ChatMessageModel>>(response);
        }
        catch (Exception ex)
        {
            return Response<List<ChatMessageModel>>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    public async Task<Response<List<ChatSessionModel>>> GetActiveSessions()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/Chat/active-sessions");
            return await HttpResponseReader.ReadAsync<List<ChatSessionModel>>(response);
        }
        catch (Exception ex)
        {
            return Response<List<ChatSessionModel>>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    public async Task<Response<ChatSessionModel>> GetSession(Guid sessionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/Chat/session/{sessionId}");
            return await HttpResponseReader.ReadAsync<ChatSessionModel>(response);
        }
        catch (Exception ex)
        {
            return Response<ChatSessionModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    public async Task<Response<bool>> CloseSession(Guid sessionId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/Chat/close/{sessionId}", null);
            return await HttpResponseReader.ReadAsync<bool>(response);
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    public async Task<Response<string>> GetSessionStatus(Guid sessionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/Chat/session/{sessionId}/status");
            return await HttpResponseReader.ReadAsync<string>(response);
        }
        catch (Exception ex)
        {
            return Response<string>.Fail(HttpResponseReader.FromException(ex));
        }
    }
}
