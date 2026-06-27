using MuuqWear.Application.Shared;
using MuuqWear.Model.Wishlist;
using System.Net.Http.Json;
using System.Text.Json;

namespace MuuqWear.Application.Services.WishlistService;

public class WishlistService : IWishlistService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;

    public WishlistService(HttpClient http)
    {
        _http = http;
    }

    public async Task<Response<List<WishlistItemModel>>> GetWishlist()
    {
        try
        {
            var httpResponse = await _http.GetAsync("api/Wishlist");
            return await ReadResponseAsync(httpResponse);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    public async Task<Response<List<WishlistItemModel>>> AddItem(Guid productId)
    {
        try
        {
            var httpResponse = await _http.PostAsJsonAsync(
                "api/Wishlist/add",
                new AddWishlistItemModel { ProductId = productId });

            return await ReadResponseAsync(httpResponse);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    public async Task<Response<List<WishlistItemModel>>> RemoveItem(Guid productId)
    {
        try
        {
            var httpResponse = await _http.DeleteAsync($"api/Wishlist/remove/{productId}");
            return await ReadResponseAsync(httpResponse);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    public async Task<Response<List<WishlistItemModel>>> Merge(List<Guid> productIds)
    {
        try
        {
            var httpResponse = await _http.PostAsJsonAsync(
                "api/Wishlist/merge",
                new MergeWishlistModel { ProductIds = productIds });

            return await ReadResponseAsync(httpResponse);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    /// <summary>
    /// API returns HTTP 200 on success and HTTP 400 on failure, both with the same JSON envelope:
    /// { "data": [...], "success": true/false, "message": "..." }
    /// </summary>
    private static async Task<Response<List<WishlistItemModel>>> ReadResponseAsync(HttpResponseMessage httpResponse)
    {
        var body = await httpResponse.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(body))
        {
            return new Response<List<WishlistItemModel>>
            {
                Success = false,
                Message = $"Empty response from server (HTTP {(int)httpResponse.StatusCode})"
            };
        }

        try
        {
            var response = JsonSerializer.Deserialize<Response<List<WishlistItemModel>>>(body, JsonOptions);
            if (response != null)
                return response;
        }
        catch
        {
            // fall through to raw body message
        }

        return new Response<List<WishlistItemModel>>
        {
            Success = false,
            Message = $"Unexpected response (HTTP {(int)httpResponse.StatusCode}): {body}"
        };
    }

    private static Response<List<WishlistItemModel>> Fail(string message) =>
        new() { Success = false, Message = message };
}
