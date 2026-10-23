using Microsoft.JSInterop;
using MuuqWear.Model.Cart;

namespace MuuqWear.Application.Services.CartService;

public class CartStateService
{
    private readonly IJSRuntime _js;

    public CartModel Cart { get; private set; } = new();
    public bool IsAuthenticated { get; private set; }

    private bool _initialized;
    private string? _initializedUserId;

    public event Action? OnCartChanged;

    public CartStateService(IJSRuntime js) => _js = js;

    public async Task InitializeAsync(bool isAuthenticated, string? userId)
    {
        if (_initialized && IsAuthenticated == isAuthenticated && _initializedUserId == userId)
            return;

        _initialized = true;
        _initializedUserId = userId;
        IsAuthenticated = isAuthenticated;
        await LoadCartFromCookie();
        NotifyCartChanged();
    }

    public async Task<bool> AddItem(AddCartItemModel request)
    {
        var existing = Cart.Items.FirstOrDefault(i =>
            i.ProductId == request.ProductId && i.Size == request.Size);

        if (existing != null)
            existing.Quantity += request.Quantity;
        else
        {
            Cart.Items.Add(new CartItemModel
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                Size = request.Size,
                Quantity = request.Quantity,
                ProductName = request.ProductName,
                ProductImageUrl = request.ProductImageUrl,
                ProductPrice = request.ProductPrice
            });
        }

        await SaveCartToCookie();
        NotifyCartChanged();
        return true;
    }

    public async Task UpdateQuantity(Guid cartItemId, int quantity)
    {
        if (quantity < 1) return;
        var item = Cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item is null) return;
        item.Quantity = quantity;
        await SaveCartToCookie();
        NotifyCartChanged();
    }

    public async Task RemoveItem(Guid cartItemId)
    {
        Cart.Items.RemoveAll(i => i.Id == cartItemId);
        await SaveCartToCookie();
        NotifyCartChanged();
    }

    public async Task ClearCart()
    {
        Cart = new CartModel();
        await SaveCartToCookie();
        NotifyCartChanged();
    }

    public Task MergeGuestCartAsync()
    {
        IsAuthenticated = true;
        NotifyCartChanged();
        return Task.CompletedTask;
    }

    public async Task LoadCart()
    {
        await LoadCartFromCookie();
        NotifyCartChanged();
    }

    public async Task LoadGuestCartFromCookie() => await LoadCartFromCookie();

    public async Task ClearCartState()
    {
        Cart = new CartModel();
        _initialized = false;
        _initializedUserId = null;
        IsAuthenticated = false;
        await ClearCartCookie();
        NotifyCartChanged();
    }

    private async Task LoadCartFromCookie()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("getCartCookie");
            if (string.IsNullOrEmpty(json))
            {
                Cart = new CartModel();
                return;
            }

            var items = System.Text.Json.JsonSerializer.Deserialize<List<CartItemModel>>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Cart = new CartModel { Items = items ?? new() };
        }
        catch
        {
            Cart = new CartModel();
        }
    }

    private async Task SaveCartToCookie()
    {
        try
        {
            await _js.InvokeVoidAsync("setCartCookie", System.Text.Json.JsonSerializer.Serialize(Cart.Items), 7);
        }
        catch { }
    }

    private async Task ClearCartCookie()
    {
        try { await _js.InvokeVoidAsync("clearCartCookie"); }
        catch { }
    }

    private void NotifyCartChanged() => OnCartChanged?.Invoke();
}
