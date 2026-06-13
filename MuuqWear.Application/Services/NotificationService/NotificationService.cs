using MuuqWear.Application.Shared;
using MuuqWear.Model.NotificationModel;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.NotificationService;

public class NotificationService : INotificationService
{
    private readonly HttpClient _http;

    public NotificationService(HttpClient http)
    {
        _http = http;
    }

    public async Task<Response<List<NotificationModel>>> GetRecent()
    {
        try
        {
            var result = await _http
                .GetFromJsonAsync<Response<List<NotificationModel>>>(
                    "api/Notification/recent");

            return result ?? new Response<List<NotificationModel>>
            {
                Success = false,
                Message = "Unexpected response from server"
            };
        }
        catch (Exception)
        {
            return new Response<List<NotificationModel>>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    public async Task MarkAllRead()
    {
        try
        {
            await _http.PostAsync(
                "api/Profile/notifications-read", null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MarkAllRead failed: {ex.Message}");
        }
    }
}
