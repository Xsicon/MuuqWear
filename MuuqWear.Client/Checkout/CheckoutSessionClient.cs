using System.Net.Http.Json;
using System.Text.Json;
using MuuqWear.Model.Cart;

namespace MuuqWear.Client.Checkout;

public sealed class CheckoutSessionClient
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;

    public CheckoutSessionClient(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("MuuqWearFunctions");
    }

    public async Task<CheckoutSessionResult> StartAsync(
        string email,
        IEnumerable<CartItemModel> items,
        string successUrl,
        string cancelUrl)
    {
        var payload = new
        {
            email,
            successUrl,
            cancelUrl,
            items = items.Select(item => new
            {
                productId = item.ProductId.ToString(),
                size = item.Size,
                color = item.Color,
                quantity = item.Quantity
            }).ToArray()
        };

        using var response = await _http.PostAsJsonAsync("createCheckoutSession", payload);
        var body = await response.Content.ReadAsStringAsync();
        CheckoutSessionResult? parsed = null;
        try
        {
            parsed = JsonSerializer.Deserialize<CheckoutSessionResult>(body, Json);
        }
        catch (JsonException)
        {
            parsed = null;
        }

        if (!response.IsSuccessStatusCode)
            return new CheckoutSessionResult { Error = parsed?.Error ?? body };

        if (string.IsNullOrWhiteSpace(parsed?.Url))
            return new CheckoutSessionResult { Error = "Checkout did not return a payment page." };

        return parsed;
    }
}

public sealed class CheckoutSessionResult
{
    public string? OrderId { get; set; }
    public string? Url { get; set; }
    public string? Error { get; set; }
}
