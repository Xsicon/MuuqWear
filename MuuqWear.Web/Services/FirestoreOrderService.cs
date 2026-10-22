using System.Security.Claims;
using MuuqWear.Application.Services.OrderService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Orders;
using MuuqWear.Model.Shared;

namespace MuuqWear.Web.Services;

public sealed class FirestoreOrderService : IOrderService
{
    private readonly AdminFirestore _store;
    private readonly IHttpContextAccessor _http;

    public FirestoreOrderService(AdminFirestore store, IHttpContextAccessor http)
    {
        _store = store;
        _http = http;
    }

    public Task<Response<OrderModel>> PlaceOrder(PlaceOrderModel request) =>
        Task.FromResult(Response<OrderModel>.Fail("Checkout is handled by Firebase Functions."));

    public async Task<Response<OrderModel>> GetOrder(Guid orderId)
    {
        var order = await _store.GetOrderAsync(orderId);
        return order is null ? Response<OrderModel>.Fail("Order not found") : Response<OrderModel>.SuccessResponse(order);
    }

    public async Task<Response<List<OrderModel>>> GetUserOrders()
    {
        var uid = _http.HttpContext?.User.FindFirst("UserId")?.Value
            ?? _http.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(uid))
            return Response<List<OrderModel>>.SuccessResponse([]);
        return Response<List<OrderModel>>.SuccessResponse(await _store.OrdersForCustomerAsync(uid));
    }

    public async Task<Response<PaginatedResponse<OrderModel>>> GetAllOrders(
        string? status, string? search, int page, int pageSize) =>
        Response<PaginatedResponse<OrderModel>>.SuccessResponse(
            await _store.ListOrdersAsync(status, search, page, pageSize));

    public Task<Response<OrderModel>> GetOrderDetail(Guid orderId) => GetOrder(orderId);

    public async Task<Response<OrderModel>> UpdateOrderStatus(Guid orderId, string status)
    {
        var order = await _store.UpdateOrderStatusAsync(orderId, status);
        return order is null ? Response<OrderModel>.Fail("Order not found") : Response<OrderModel>.SuccessResponse(order);
    }

    public async Task<Response<int>> BulkUpdateOrderStatus(List<Guid> orderIds, string status)
    {
        var count = 0;
        foreach (var id in orderIds)
        {
            if (await _store.UpdateOrderStatusAsync(id, status) is not null)
                count++;
        }
        return Response<int>.SuccessResponse(count);
    }
}
