using MuuqWear.Application.Shared;
using MuuqWear.Model.Payment;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.PaymentService;

public class PaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;

    public PaymentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Response<CreatePaymentIntentResultModel>> CreateIntent(Guid orderId)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"api/Payment/create-intent/{orderId}", null);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content
                    .ReadFromJsonAsync<Response<CreatePaymentIntentResultModel>>();
                return err ?? Response<CreatePaymentIntentResultModel>.Fail(
                    "Failed to create payment intent");
            }

            var result = await response.Content
                .ReadFromJsonAsync<Response<CreatePaymentIntentResultModel>>();
            return result ?? Response<CreatePaymentIntentResultModel>.Fail("Invalid response");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Frontend Payment] CreateIntent error: {ex.Message}");
            return Response<CreatePaymentIntentResultModel>.Fail($"Error: {ex.Message}");
        }
    }
}
