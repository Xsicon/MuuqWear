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

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<Response<ChatMessageModel>>();
                return error ?? Response<ChatMessageModel>.Fail("Failed to send message");
            }

            var result = await response.Content.ReadFromJsonAsync<Response<ChatMessageModel>>();
            return result ?? Response<ChatMessageModel>.Fail("Invalid response");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Frontend Chat] SendMessage error: {ex.Message}");
            return Response<ChatMessageModel>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<Response<List<ChatMessageModel>>> GetMessages(Guid sessionId)
    {
        try
        {
            var result = await _httpClient
                .GetFromJsonAsync<Response<List<ChatMessageModel>>>($"api/Chat/messages/{sessionId}");

            return result ?? Response<List<ChatMessageModel>>.Fail("Invalid response");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Frontend Chat] GetMessages error: {ex.Message}");
            return Response<List<ChatMessageModel>>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<Response<List<ChatSessionModel>>> GetActiveSessions()
    {
        try
        {
            var result = await _httpClient
                .GetFromJsonAsync<Response<List<ChatSessionModel>>>("api/Chat/active-sessions");

            return result ?? Response<List<ChatSessionModel>>.Fail("Invalid response");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Frontend Chat] GetActiveSessions error: {ex.Message}");
            return Response<List<ChatSessionModel>>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<Response<bool>> CloseSession(Guid sessionId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/Chat/close/{sessionId}", null);

            if (!response.IsSuccessStatusCode)
                return Response<bool>.Fail("Failed to close session");

            var result = await response.Content.ReadFromJsonAsync<Response<bool>>();
            return result ?? Response<bool>.Fail("Invalid response");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Frontend Chat] CloseSession error: {ex.Message}");
            return Response<bool>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<Response<string>> GetSessionStatus(Guid sessionId)
    {
        try
        {
            var result = await _httpClient
                .GetFromJsonAsync<Response<string>>($"api/Chat/session/{sessionId}/status");
            return result ?? Response<string>.Fail("Invalid response");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Frontend Chat] GetSessionStatus error: {ex.Message}");
            return Response<string>.Fail($"Error: {ex.Message}");
        }
    }

}
