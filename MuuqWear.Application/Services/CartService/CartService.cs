using MuuqWear.Application.Shared;
using MuuqWear.Model.Cart;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.CartService;
public class CartService : ICartService
{
    private readonly HttpClient _http;

    public CartService(HttpClient http)
    {
        _http = http;
    }

    public async Task<Response<CartModel>> GetCart()
    {
        try
        {
            var result = await _http
                .GetFromJsonAsync<Response<CartModel>>("api/Cart");

            return result ?? new Response<CartModel>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<CartModel> { Success = false, Message = ex.Message };
        }
    }

    public async Task<Response<CartModel>> AddItem(AddCartItemModel request)
    {
        var result = await _http.PostAsJsonAsync("api/Cart/add", request);
        if (!result.IsSuccessStatusCode)
            return new Response<CartModel> { Success = false, Message = $"Server error: {result.StatusCode}" };

        var response = await result.Content.ReadFromJsonAsync<Response<CartModel>>();
        return response ?? new Response<CartModel> { Success = false, Message = "Empty response from server" };
    }

    public async Task<Response<CartModel>> UpdateQuantity(UpdateCartItemModel request)
    {
        var result = await _http.PutAsJsonAsync("api/Cart/update", request);
        if (!result.IsSuccessStatusCode)
            return new Response<CartModel> { Success = false, Message = $"Server error: {result.StatusCode}" };

        var response = await result.Content.ReadFromJsonAsync<Response<CartModel>>();
        return response ?? new Response<CartModel> { Success = false, Message = "Empty response from server" };
    }

    public async Task<Response<CartModel>> RemoveItem(Guid cartItemId)
    {
        var result = await _http.DeleteAsync($"api/Cart/remove/{cartItemId}");
        if (!result.IsSuccessStatusCode)
            return new Response<CartModel> { Success = false, Message = $"Server error: {result.StatusCode}" };

        var response = await result.Content.ReadFromJsonAsync<Response<CartModel>>();
        return response ?? new Response<CartModel> { Success = false, Message = "Empty response from server" };
    }

    public async Task<Response<CartModel>> ClearCart()
    {
        var result = await _http.DeleteAsync("api/Cart/clear");
        if (!result.IsSuccessStatusCode)
            return new Response<CartModel> { Success = false, Message = $"Server error: {result.StatusCode}" };

        var response = await result.Content.ReadFromJsonAsync<Response<CartModel>>();
        return response ?? new Response<CartModel> { Success = false, Message = "Empty response from server" };
    }

    public async Task<Response<CartModel>> MergeCart(List<AddCartItemModel> guestItems)
    {
        var result = await _http.PostAsJsonAsync("api/Cart/merge", guestItems);
        if (!result.IsSuccessStatusCode)
            return new Response<CartModel> { Success = false, Message = $"Server error: {result.StatusCode}" };

        var response = await result.Content.ReadFromJsonAsync<Response<CartModel>>();
        return response ?? new Response<CartModel> { Success = false, Message = "Empty response from server" };
    }
}