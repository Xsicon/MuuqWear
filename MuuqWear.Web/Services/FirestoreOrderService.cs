using MuuqWear.Application.Services.OrderService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Orders;
using MuuqWear.Model.Shared;
using MuuqWear.Web.Helpers;
using MuuqWear.Web.ServerAuth;

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
        if (order is null || !OrderAccess.CanView(order, _http.HttpContext?.User))
            return Response<OrderModel>.Fail("Order not found");
        return Response<OrderModel>.SuccessResponse(order);
    }

    public async Task<Response<List<OrderModel>>> GetUserOrders()
    {
        var (uid, _, _) = VerifiedAuthSession.Shopper(_http.HttpContext?.User);
        if (string.IsNullOrWhiteSpace(uid))
            return Response<List<OrderModel>>.Fail("Sign in to view your orders.");
        return Response<List<OrderModel>>.SuccessResponse(await _store.OrdersForCustomerAsync(uid));
    }

    public async Task<Response<PaginatedResponse<OrderModel>>> GetAllOrders(
        string? status, string? search, int page, int pageSize)
    {
        if (!OrderAccess.IsStaff(_http.HttpContext?.User))
            return Response<PaginatedResponse<OrderModel>>.Fail("Not allowed");
        return Response<PaginatedResponse<OrderModel>>.SuccessResponse(
            await _store.ListOrdersAsync(status, search, page, pageSize));
    }

    public Task<Response<OrderModel>> GetOrderDetail(Guid orderId) => GetOrder(orderId);

    public async Task<Response<OrderModel>> UpdateOrderStatus(Guid orderId, string status)
    {
        if (!OrderAccess.IsStaff(_http.HttpContext?.User))
            return Response<OrderModel>.Fail("Not allowed");
        var order = await _store.UpdateOrderStatusAsync(orderId, status);
        return order is null ? Response<OrderModel>.Fail("Order not found") : Response<OrderModel>.SuccessResponse(order);
    }

    public async Task<Response<int>> BulkUpdateOrderStatus(List<Guid> orderIds, string status)
    {
        if (!OrderAccess.IsStaff(_http.HttpContext?.User))
            return Response<int>.Fail("Not allowed");
        var count = 0;
        foreach (var id in orderIds)
        {
            if (await _store.UpdateOrderStatusAsync(id, status) is not null)
                count++;
        }
        return Response<int>.SuccessResponse(count);
    }
}
