using MuuqWear.Application.Shared;
using MuuqWear.Model.Payment;

namespace MuuqWear.Application.Services.PaymentService;

public interface IPaymentService
{
    Task<Response<CreatePaymentIntentResultModel>> CreateIntent(Guid orderId);
}
