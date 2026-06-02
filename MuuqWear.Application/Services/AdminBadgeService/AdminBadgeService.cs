using MuuqWear.Application.Shared;
using MuuqWear.Model.AdminBadgeCount;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.AdminBadgeService;

public class AdminBadgeService : IAdminBadgeService
{
    private readonly HttpClient _http;

    public AdminBadgeService(HttpClient http)
    {
        _http = http;
    }

    public async Task<Response<AdminBadgeCountsModel>> GetCounts()
    {
        try
        {
            var result = await _http.GetAsync("api/AdminBadge/counts");
            var response = await result.Content
                .ReadFromJsonAsync<Response<AdminBadgeCountsModel>>();

            return response ?? new Response<AdminBadgeCountsModel>
            {
                Success = false,
                Message = "Unexpected response from server"
            };
        }
        catch (Exception)
        {
            return new Response<AdminBadgeCountsModel>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }
}
