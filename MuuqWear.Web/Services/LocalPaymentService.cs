using MuuqWear.Application.Services.PaymentService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Payment;

namespace MuuqWear.Web.Services;

public sealed class LocalPaymentService : IPaymentService
{
    public Task<Response<CreatePaymentIntentResultModel>> CreateIntent(Guid orderId) =>
        Task.FromResult(Response<CreatePaymentIntentResultModel>.Fail(
            "Pay on Stripe checkout. Card details never touch this site."));
}
