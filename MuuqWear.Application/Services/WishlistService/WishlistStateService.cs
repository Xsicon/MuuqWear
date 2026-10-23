using Microsoft.JSInterop;
using MuuqWear.Model.Wishlist;

namespace MuuqWear.Application.Services.WishlistService;

public class WishlistStateService
{
    private readonly IJSRuntime _js;

    public List<WishlistItemModel> Items { get; private set; } = new();
    public int Count => Items.Count;
    public bool IsAuthenticated { get; private set; }
    public event Action? OnWishlistChanged;

    private bool _initialized;
    private string? _initializedUserId;

    public WishlistStateService(IJSRuntime js) => _js = js;

    public async Task InitializeAsync(bool isAuthenticated, string? userId)
    {
        if (_initialized && IsAuthenticated == isAuthenticated && _initializedUserId == userId)
            return;

        _initialized = true;
        _initializedUserId = userId;
        IsAuthenticated = isAuthenticated;
        await LoadFromCookieAsync();
        NotifyChanged();
    }

    public bool IsWishlisted(Guid productId) =>
        Items.Any(i => i.ProductId == productId);

    public async Task<bool> ToggleAsync(WishlistItemModel item)
    {
        var wasActive = IsWishlisted(item.ProductId);
        if (wasActive)
            Items.RemoveAll(i => i.ProductId == item.ProductId);
        else
        {
            item.AddedAt = DateTime.UtcNow;
            Items.Add(EnsureId(item));
        }

        await SaveToCookieAsync();
        NotifyChanged();
        return IsWishlisted(item.ProductId);
    }

    public async Task RemoveAsync(Guid productId)
    {
        if (Items.RemoveAll(i => i.ProductId == productId) == 0)
            return;
        await SaveToCookieAsync();
        NotifyChanged();
    }

    public async Task RefreshAsync()
    {
        await LoadFromCookieAsync();
        NotifyChanged();
    }

    public Task LoadGuestWishlistFromCookie() => LoadFromCookieAsync();

    public Task MergeGuestWishlistAsync()
    {
        IsAuthenticated = true;
        NotifyChanged();
        return Task.CompletedTask;
    }

    public async Task ClearWishlistState()
    {
        Items = new();
        _initialized = false;
        _initializedUserId = null;
        IsAuthenticated = false;
        await ClearCookieAsync();
        NotifyChanged();
    }

    private static WishlistItemModel EnsureId(WishlistItemModel item)
    {
        if (item.Id == Guid.Empty)
            item.Id = Guid.NewGuid();
        return item;
    }

    private async Task LoadFromCookieAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("getWishlistCookie");
            if (string.IsNullOrEmpty(json))
            {
                Items = new();
                return;
            }

            var items = System.Text.Json.JsonSerializer.Deserialize<List<WishlistItemModel>>(
                json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Items = items ?? new();
        }
        catch
        {
            Items = new();
        }
    }

    private async Task SaveToCookieAsync()
    {
        try { await _js.InvokeVoidAsync("setWishlistCookie", System.Text.Json.JsonSerializer.Serialize(Items), 30); }
        catch { }
    }

    private async Task ClearCookieAsync()
    {
        try { await _js.InvokeVoidAsync("clearWishlistCookie"); }
        catch { }
    }

    private void NotifyChanged() => OnWishlistChanged?.Invoke();
}
