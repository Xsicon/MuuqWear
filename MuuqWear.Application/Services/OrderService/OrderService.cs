using System.Net.Http.Json;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Orders;

namespace MuuqWear.Application.Services.OrderService;

public class OrderService : IOrderService
{
    private readonly HttpClient _http;

    public OrderService(HttpClient http)
    {
        _http = http;
    }

    public async Task<Response<OrderModel>> PlaceOrder(PlaceOrderModel request)
    {
        var result = await _http.PostAsJsonAsync("api/Order/place", request);
        if (!result.IsSuccessStatusCode)
            return new Response<OrderModel>
            {
                Success = false,
                Message = $"Server error: {result.StatusCode}"
            };

        var response = await result.Content
            .ReadFromJsonAsync<Response<OrderModel>>();

        return response ?? new Response<OrderModel>
        {
            Success = false,
            Message = "Empty response from server"
        };
    }

    public async Task<Response<OrderModel>> GetOrder(Guid orderId)
    {
        try
        {
            var result = await _http
                .GetFromJsonAsync<Response<OrderModel>>(
                    $"api/Order/{orderId}");

            return result ?? new Response<OrderModel>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<OrderModel>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<Response<List<OrderModel>>> GetUserOrders()
    {
        try
        {
            var result = await _http
                .GetFromJsonAsync<Response<List<OrderModel>>>(
                    "api/Order/my-orders");

            return result ?? new Response<List<OrderModel>>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<List<OrderModel>>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
}