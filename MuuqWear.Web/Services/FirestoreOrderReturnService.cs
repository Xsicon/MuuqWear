using MuuqWear.Application.Services.OrderReturnService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.OrderReturn;
using MuuqWear.Model.Shared;

namespace MuuqWear.Web.Services;

public sealed class FirestoreOrderReturnService : IOrderReturnService
{
    private readonly AdminFirestore _store;

    public FirestoreOrderReturnService(AdminFirestore store) => _store = store;

    public async Task<Response<OrderReturnModel>> SubmitReturn(SubmitReturnModel request) =>
        Response<OrderReturnModel>.SuccessResponse(await _store.SubmitReturnAsync(request));

    public async Task<Response<PaginatedResponse<OrderReturnModel>>> GetAllReturns(
        string? status, int page, int pageSize) =>
        Response<PaginatedResponse<OrderReturnModel>>.SuccessResponse(
            await _store.ListReturnsAsync(status, page, pageSize));

    public async Task<Response<OrderReturnModel>> UpdateReturnStatus(Guid returnId, string status)
    {
        var updated = await _store.UpdateReturnStatusAsync(returnId, status);
        return updated is null
            ? Response<OrderReturnModel>.Fail("Return not found")
            : Response<OrderReturnModel>.SuccessResponse(updated);
    }
}
