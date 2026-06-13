using Microsoft.JSInterop;
using MuuqWear.Application.Services.ProductService;
using MuuqWear.Model.Cart;

namespace MuuqWear.Application.Services.CartService;

/// </summary>
public class CartStateService
{
    private readonly ICartService _cartService;
    private readonly IJSRuntime _js;

    // current cart state
    public CartModel Cart { get; private set; } = new();

    // true when user is logged in 
    public bool IsAuthenticated { get; private set; } = false;

    private bool _initialized;
    private string? _initializedUserId;

    // fired when cart changes → UI re-renders 
    public event Action? OnCartChanged;

    public CartStateService(ICartService cartService, IJSRuntime js)
    {
        _cartService = cartService;
        _js = js;
        ;
    }

    // =============================================
    // INITIALIZE — called on app start
    // loads cart based on auth state
    // =============================================
    public async Task InitializeAsync(bool isAuthenticated, string? userId)
    {
        if (_initialized && IsAuthenticated == isAuthenticated && _initializedUserId == userId)
            return;

        _initialized = true;
        _initializedUserId = userId;
        IsAuthenticated = isAuthenticated;

        if (isAuthenticated)
        {
            // logged in → load from DB
            await LoadCartFromDb();
        }
        else
        {
            // guest → load from cookie
            await LoadCartFromCookie();
        }
    }

    // =============================================
    // ADD ITEM
    // =============================================
    public async Task<bool> AddItem(AddCartItemModel request)
    {
        if (IsAuthenticated)
        {
            // logged in → save to DB
            var result = await _cartService.AddItem(request);
            if (result.Success && result.Data != null)
            {
                Cart = result.Data;
                NotifyCartChanged();
                return true;
            }
            return false;
        }
        else
        {

            // guest → save to cookie
            var existing = Cart.Items.FirstOrDefault(i =>
                i.ProductId == request.ProductId &&
                i.Size == request.Size);

            if (existing != null)
            {
                // increase quantity
                existing.Quantity += request.Quantity;
            }
            else
            {
                // add new item
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
    }

    // =============================================
    // UPDATE QUANTITY
    // =============================================
    public async Task UpdateQuantity(Guid cartItemId, int quantity)
    {
        if (quantity < 1) return;

        if (IsAuthenticated)
        {
            var result = await _cartService.UpdateQuantity(
                new UpdateCartItemModel
                {
                    CartItemId = cartItemId,
                    Quantity = quantity
                });

            if (result.Success && result.Data != null)
            {
                Cart = result.Data;
                NotifyCartChanged();
            }
        }
        else
        {
            // guest → update in memory + cookie 
            var item = Cart.Items.FirstOrDefault(i => i.Id == cartItemId);
            if (item != null)
            {
                item.Quantity = quantity;
                await SaveCartToCookie();
                NotifyCartChanged();
            }
        }
    }

    // =============================================
    // REMOVE ITEM
    // =============================================
    public async Task RemoveItem(Guid cartItemId)
    {
        if (IsAuthenticated)
        {
            var result = await _cartService.RemoveItem(cartItemId);
            if (result.Success && result.Data != null)
            {
                Cart = result.Data;
                NotifyCartChanged();
            }
        }
        else
        {
            // guest → remove from memory + cookie 
            Cart.Items.RemoveAll(i => i.Id == cartItemId);
            await SaveCartToCookie();
            NotifyCartChanged();
        }
    }

    // =============================================
    // CLEAR CART
    // =============================================
    public async Task ClearCart()
    {
        if (IsAuthenticated)
        {
            var result = await _cartService.ClearCart();
            if (result.Success)
            {
                Cart = new CartModel();
                NotifyCartChanged();
            }
        }
        else
        {
            Cart = new CartModel();
            await SaveCartToCookie();
            NotifyCartChanged();
        }
    }

    // =============================================
    // MERGE — called after login
    // merges guest cookie cart into DB cart
    // =============================================
    public async Task MergeGuestCartAsync()
    {
        // get guest items currently in Cart
        var guestItems = Cart.Items.Select(i => new AddCartItemModel
        {
            ProductId = i.ProductId,
            Size = i.Size,
            Quantity = i.Quantity,
            ProductName = i.ProductName,
            ProductImageUrl = i.ProductImageUrl,
            ProductPrice = i.ProductPrice
        }).ToList();

        // set authenticated before API call 
        IsAuthenticated = true;

        if (guestItems.Any())
        {
            // merge into DB 
            var result = await _cartService.MergeCart(guestItems);
            if (result.Success && result.Data != null)
            {
                Cart = result.Data;
            }
            else
            {
                await LoadCartFromDb();
            }
        }
        else
        {
            await LoadCartFromDb();
        }

        await ClearCartCookie();
        NotifyCartChanged();
    }
    public async Task LoadCart()
    {
        if (IsAuthenticated)
            await LoadCartFromDb();
        else
            await LoadCartFromCookie();

        NotifyCartChanged();
    }
    // =============================================
    // PRIVATE HELPERS
    // =============================================

    // load cart from DB 
    private async Task LoadCartFromDb()
    {
        var result = await _cartService.GetCart();
        if (result.Success && result.Data != null)
            Cart = result.Data;
        else
            Cart = new CartModel();
    }

    // load cart from cookie 
    private async Task LoadCartFromCookie()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>(
                "getCartCookie");

            if (!string.IsNullOrEmpty(json))
            {
                var items = System.Text.Json.JsonSerializer
                    .Deserialize<List<CartItemModel>>(json,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                Cart = new CartModel { Items = items ?? new() };
            }
            else
            {
                Cart = new CartModel();
            }
        }
        catch
        {
            // cookie corrupt → start fresh 
            Cart = new CartModel();
        }
    }

    public async Task LoadGuestCartFromCookie()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("getCartCookie");
            if (!string.IsNullOrEmpty(json))
            {
                var items = System.Text.Json.JsonSerializer
                    .Deserialize<List<CartItemModel>>(json,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                Cart = new CartModel { Items = items ?? new() };
            }
        }
        catch
        {
            Cart = new CartModel();
        }
    }
    public async Task ClearCartState()
    {
        Cart = new CartModel();
        _initialized = false;
        _initializedUserId = null;
        IsAuthenticated = false;
        await ClearCartCookie();
        NotifyCartChanged();
    }
    // save cart to cookie 
    private async Task SaveCartToCookie()
    {
        try
        {
            var json = System.Text.Json.JsonSerializer
                .Serialize(Cart.Items);

            await _js.InvokeVoidAsync("setCartCookie", json, 7);
        }
        catch
        {
            // ignore cookie errors 
        }
    }

    // clear cart cookie 
    private async Task ClearCartCookie()
    {
        try
        {
            await _js.InvokeVoidAsync("clearCartCookie");
        }
        catch { }
    }

    // notify all subscribers → UI re-renders 
    private void NotifyCartChanged()
    {
        OnCartChanged?.Invoke();
    }
}
