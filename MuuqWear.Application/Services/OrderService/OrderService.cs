using MuuqWear.Application.Shared;
using MuuqWear.Model.Orders;
using MuuqWear.Model.Shared;
using System.Net.Http.Json;

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
    // =============================================
    // GET ALL ORDERS (ADMIN)
    // =============================================
    public async Task<Response<PaginatedResponse<OrderModel>>> GetAllOrders(
        string? status, string? search, int page, int pageSize)
    {
        try
        {
            var url = $"api/Order/admin?page={page}&pageSize={pageSize}";

            if (!string.IsNullOrEmpty(status))
                url += $"&status={Uri.EscapeDataString(status)}";

            if (!string.IsNullOrEmpty(search))
                url += $"&search={Uri.EscapeDataString(search)}";

            var result = await _http
                .GetFromJsonAsync<Response<PaginatedResponse<OrderModel>>>(url);

            return result ?? new Response<PaginatedResponse<OrderModel>>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<PaginatedResponse<OrderModel>>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    // =============================================
    // GET ORDER DETAIL (ADMIN)
    // =============================================
    public async Task<Response<OrderModel>> GetOrderDetail(Guid orderId)
    {
        try
        {
            var result = await _http
                .GetFromJsonAsync<Response<OrderModel>>(
                    $"api/Order/admin/{orderId}");

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

    // =============================================
    // UPDATE ORDER STATUS (ADMIN)
    // =============================================
    public async Task<Response<OrderModel>> UpdateOrderStatus(
        Guid orderId, string status)
    {
        try
        {
            var result = await _http.PatchAsJsonAsync(
                $"api/Order/admin/{orderId}/status",
                new UpdateOrderStatusModel { Status = status });

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
        catch (Exception ex)
        {
            return new Response<OrderModel>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    // =============================================
    // BULK UPDATE ORDER STATUS (ADMIN)
    // =============================================
    public async Task<Response<int>> BulkUpdateOrderStatus(
        List<Guid> orderIds, string status)
    {
        try
        {
            var result = await _http.PatchAsJsonAsync(
                "api/Order/admin/bulk-status",
                new BulkUpdateOrderStatusModel
                {
                    OrderIds = orderIds,
                    Status = status
                });

            if (!result.IsSuccessStatusCode)
                return new Response<int>
                {
                    Success = false,
                    Message = $"Server error: {result.StatusCode}"
                };

            var response = await result.Content
                .ReadFromJsonAsync<Response<int>>();

            return response ?? new Response<int>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<int>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
}