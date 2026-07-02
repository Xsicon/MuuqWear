using MuuqWear.Application.Shared;
using MuuqWear.Model.Refund;
using MuuqWear.Model.Shared;

namespace MuuqWear.Application.Services.RefundService;

public interface IRefundService
{
    Task<Response<PaginatedResponse<RefundModel>>> GetAllRefunds(
        string? status, int page, int pageSize);

    Task<Response<RefundModel>> ProcessRefund(Guid refundId);
}
