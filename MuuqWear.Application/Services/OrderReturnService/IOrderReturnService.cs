using MuuqWear.Application.Shared;
using MuuqWear.Model.OrderReturn;
using MuuqWear.Model.Shared;

namespace MuuqWear.Application.Services.OrderReturnService;

public interface IOrderReturnService
{
    Task<Response<OrderReturnModel>> SubmitReturn(SubmitReturnModel request);
    Task<Response<PaginatedResponse<OrderReturnModel>>> GetAllReturns(
        string? status, int page, int pageSize);
    Task<Response<OrderReturnModel>> UpdateReturnStatus(
        Guid returnId, string status);
}
