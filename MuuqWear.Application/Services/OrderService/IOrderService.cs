using MuuqWear.Application.Shared;
using MuuqWear.Model.Orders;
using MuuqWear.Model.Shared;

namespace MuuqWear.Application.Services.OrderService;

public interface IOrderService
{
    Task<Response<OrderModel>> PlaceOrder(PlaceOrderModel request);
    Task<Response<OrderModel>> GetOrder(Guid orderId);
    Task<Response<List<OrderModel>>> GetUserOrders();

    // ← new admin methods

    Task<Response<PaginatedResponse<OrderModel>>> GetAllOrders(
       string? status, string? search, int page, int pageSize);
    Task<Response<OrderModel>> GetOrderDetail(Guid orderId);
    Task<Response<OrderModel>> UpdateOrderStatus(Guid orderId, string status);
    Task<Response<int>> BulkUpdateOrderStatus(
    List<Guid> orderIds, string status);
}