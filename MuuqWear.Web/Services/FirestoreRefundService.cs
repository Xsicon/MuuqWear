using MuuqWear.Application.Services.RefundService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Refund;
using MuuqWear.Model.Shared;

namespace MuuqWear.Web.Services;

public sealed class FirestoreRefundService : IRefundService
{
    private readonly AdminFirestore _store;

    public FirestoreRefundService(AdminFirestore store) => _store = store;

    public async Task<Response<PaginatedResponse<RefundModel>>> GetAllRefunds(
        string? status, int page, int pageSize) =>
        Response<PaginatedResponse<RefundModel>>.SuccessResponse(
            await _store.ListRefundsAsync(status, page, pageSize));

    public async Task<Response<RefundModel>> ProcessRefund(Guid refundId)
    {
        var updated = await _store.ProcessRefundAsync(refundId);
        return updated is null
            ? Response<RefundModel>.Fail("Refund not found")
            : Response<RefundModel>.SuccessResponse(updated);
    }
}
