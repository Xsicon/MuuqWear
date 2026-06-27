using Microsoft.JSInterop;
using MuuqWear.Model.Wishlist;

namespace MuuqWear.Application.Services.WishlistService;

public class WishlistStateService
{
    private readonly IWishlistService _wishlistService;
    private readonly IJSRuntime _js;

    public List<WishlistItemModel> Items { get; private set; } = new();

    public int Count => Items.Count;

    public bool IsAuthenticated { get; private set; }

    public event Action? OnWishlistChanged;

    private bool _initialized;
    private string? _initializedUserId;

    public WishlistStateService(IWishlistService wishlistService, IJSRuntime js)
    {
        _wishlistService = wishlistService;
        _js = js;
    }

    public async Task InitializeAsync(bool isAuthenticated, string? userId)
    {
        if (_initialized && IsAuthenticated == isAuthenticated && _initializedUserId == userId)
            return;

        _initialized = true;
        _initializedUserId = userId;
        IsAuthenticated = isAuthenticated;

        if (isAuthenticated)
            await LoadWishlistFromApiAsync();
        else
            await LoadFromCookieAsync();

        NotifyChanged();
    }

    public bool IsWishlisted(Guid productId) =>
        Items.Any(i => i.ProductId == productId);

    public async Task<bool> ToggleAsync(WishlistItemModel item)
    {
        var wasActive = IsWishlisted(item.ProductId);

        if (IsAuthenticated)
        {
            // Optimistic update so the UI responds immediately.
            if (wasActive)
                Items.RemoveAll(i => i.ProductId == item.ProductId);
            else
                Items.Add(EnsureId(item));

            NotifyChanged();

            var result = wasActive
                ? await _wishlistService.RemoveItem(item.ProductId)
                : await _wishlistService.AddItem(item.ProductId);

            if (result.Success)
            {
                if (result.Data != null)
                    Items = result.Data;
                else
                    await LoadWishlistFromApiAsync();
            }
            else
            {
                // Roll back to the authoritative server state on failure.
                await LoadWishlistFromApiAsync();
            }

            NotifyChanged();
            return IsWishlisted(item.ProductId);
        }

        if (wasActive)
        {
            Items.RemoveAll(i => i.ProductId == item.ProductId);
            await SaveToCookieAsync();
            NotifyChanged();
            return false;
        }

        item.AddedAt = DateTime.UtcNow;
        Items.Add(EnsureId(item));
        await SaveToCookieAsync();
        NotifyChanged();
        return true;
    }

    public async Task RemoveAsync(Guid productId)
    {
        if (IsAuthenticated)
        {
            Items.RemoveAll(i => i.ProductId == productId);
            NotifyChanged();

            var result = await _wishlistService.RemoveItem(productId);
            if (result.Success && result.Data != null)
                Items = result.Data;
            else
                await LoadWishlistFromApiAsync();

            NotifyChanged();
            return;
        }

        if (Items.RemoveAll(i => i.ProductId == productId) == 0)
            return;

        await SaveToCookieAsync();
        NotifyChanged();
    }

    public async Task RefreshAsync()
    {
        if (IsAuthenticated)
            await LoadWishlistFromApiAsync();
        else
            await LoadFromCookieAsync();

        NotifyChanged();
    }

    public async Task LoadGuestWishlistFromCookie()
    {
        await LoadFromCookieAsync();
    }

    public async Task MergeGuestWishlistAsync()
    {
        var guestProductIds = Items.Select(i => i.ProductId).Distinct().ToList();
        IsAuthenticated = true;

        if (guestProductIds.Any())
        {
            var result = await _wishlistService.Merge(guestProductIds);
            if (result.Success && result.Data != null)
                Items = result.Data;
            else
                await LoadWishlistFromApiAsync();
        }
        else
        {
            await LoadWishlistFromApiAsync();
        }

        await ClearCookieAsync();
        NotifyChanged();
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

    private async Task LoadWishlistFromApiAsync()
    {
        var result = await _wishlistService.GetWishlist();
        if (result.Success)
            Items = result.Data ?? new();
        // On failure keep the current items instead of wiping them.
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
                json,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            Items = items ?? new();
        }
        catch
        {
            Items = new();
        }
    }

    private async Task SaveToCookieAsync()
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(Items);
            await _js.InvokeVoidAsync("setWishlistCookie", json, 30);
        }
        catch
        {
            // ignore cookie errors
        }
    }

    private async Task ClearCookieAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("clearWishlistCookie");
        }
        catch
        {
            // ignore cookie errors
        }
    }

    private void NotifyChanged() => OnWishlistChanged?.Invoke();
}
