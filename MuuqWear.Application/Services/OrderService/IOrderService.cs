using MuuqWear.Application.Shared;
using MuuqWear.Model.Orders;

namespace MuuqWear.Application.Services.OrderService;

public interface IOrderService
{
    Task<Response<OrderModel>> PlaceOrder(PlaceOrderModel request);
    Task<Response<OrderModel>> GetOrder(Guid orderId);
    Task<Response<List<OrderModel>>> GetUserOrders();
}