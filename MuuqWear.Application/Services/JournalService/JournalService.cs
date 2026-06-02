using MuuqWear.Application.Shared;
using MuuqWear.Model.ContentItem;
using MuuqWear.Model.Shared;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.JournalService;

public class JournalService : IJournalService
{
    private readonly HttpClient _http;

    public JournalService(HttpClient http)
    {
        _http = http;
    }

    // =============================================
    // GET ALL PUBLISHED
    // =============================================
    public async Task<Response<List<ContentItemModel>>> GetPublished()
    {
        try
        {
            var result = await _http.GetAsync("api/Journal");
            return await ReadResponse<List<ContentItemModel>>(result);
        }
        catch (Exception ex)
        {
            return Response<List<ContentItemModel>>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET BY ID
    // =============================================
    public async Task<Response<ContentItemModel>> GetById(Guid id)
    {
        try
        {
            var result = await _http.GetAsync($"api/Journal/{id}");
            return await ReadResponse<ContentItemModel>(result);
        }
        catch (Exception ex)
        {
            return Response<ContentItemModel>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // HELPER
    // =============================================
    private async Task<Response<T>> ReadResponse<T>(HttpResponseMessage response)
    {
        try
        {
            var result = await response.Content
                .ReadFromJsonAsync<Response<T>>();
            return result ?? Response<T>.Fail("Empty response");
        }
        catch
        {
            return Response<T>.Fail("Failed to parse response");
        }
    }

    public async Task<Response<PaginatedResponse<ContentItemModel>>> GetPublished(
     int page = 1, int pageSize = 6, string? category = null)
    {
        try
        {
            var url = $"api/Journal?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(category))
                url += $"&category={Uri.EscapeDataString(category)}";

            var result = await _http.GetAsync(url);

            //  debug — see raw response
            var json = await result.Content.ReadAsStringAsync();

            if (!result.IsSuccessStatusCode)
                return new Response<PaginatedResponse<ContentItemModel>>
                {
                    Success = false,
                    Message = $"Server error: {result.StatusCode}"
                };

            var response = await System.Net.Http.Json.HttpContentJsonExtensions
                .ReadFromJsonAsync<Response<PaginatedResponse<ContentItemModel>>>(
                    new System.Net.Http.StringContent(json,
                        System.Text.Encoding.UTF8, "application/json"));

            return response ?? new Response<PaginatedResponse<ContentItemModel>>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<PaginatedResponse<ContentItemModel>>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
}
