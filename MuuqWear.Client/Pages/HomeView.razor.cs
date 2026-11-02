using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MuuqWear.Web.Services;

namespace MuuqWear.Client.Pages;

public partial class HomeView : IAsyncDisposable
{
    private const int HeroAutoplayMs = 5500;

    private record HeroSlide(string Image, string Eyebrow, string Headline, string Subline, string Cta, string Link);

    private record CategoryCard(string Name, string Image, string Link);

    [Inject] private StorefrontHomeCacheService HomeCache { get; set; } = default!;

    private List<HeroSlide> HeroSlides { get; } = new()
    {
        new(
            "https://images.unsplash.com/photo-1523381210434-271e8be1f52b?w=1600&h=900&fit=crop",
            "Limited Release",
            "The Sapphire Collection",
            "",
            "Explore the Collection",
            "/shop"),
        new(
            "https://images.unsplash.com/photo-1756361771567-e276865b77cb?w=1400&h=700&fit=crop",
            "Limited Release",
            "Texture. Refined.",
            "",
            "Discover Materials",
            "/shop/apparel"),
        new(
            "https://images.unsplash.com/photo-1761957375235-46acb4862151?w=1400&h=700&fit=crop",
            "Limited Release",
            "Built by Community",
            "",
            "Join the Movement",
            "/community")
    };

    private List<CategoryCard> Categories = new();
    private List<MuuqWear.Model.Products.ProductModel> NewArrivals = new();
    private List<MuuqWear.Model.Products.ProductModel> FeaturedProducts = new();
    private List<MuuqWear.Model.Products.ProductModel> BestSellerProducts = new();

    private bool _homeCarouselsReady;
    private bool isLoadingProducts = true;
    private bool isLoadingCategories = true;
    private bool _categoriesRequested;
    private bool _productsRequested;
    private bool _lazySectionsWired;
    private DotNetObjectReference<HomeView>? _dotNetRef;

    private const string CategoryImageFallback =
        "https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=600";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                await JS.InvokeVoidAsync("mwInitHeroCarousel", HeroAutoplayMs);
            }
            catch (JSDisconnectedException) { }
            catch (InvalidOperationException) { }

            var fragment = await JS.InvokeAsync<string>(
                "eval", "window.location.hash");

            if (!string.IsNullOrEmpty(fragment) &&
                fragment.Contains("type=recovery"))
            {
                await JS.InvokeVoidAsync("eval",
                    $"window.location.href = '/auth/reset-password' + window.location.hash");
                return;
            }

            await WireLazySectionsAsync();
        }

        if (!_homeCarouselsReady &&
            (Categories.Count > 0 || NewArrivals.Count > 0 || FeaturedProducts.Count > 0 || BestSellerProducts.Count > 0))
        {
            try
            {
                await JS.InvokeVoidAsync("mwDestroyHomeCarousels");
                await JS.InvokeVoidAsync("mwInitHomeCarousels");
                _homeCarouselsReady = true;
            }
            catch (JSDisconnectedException) { }
            catch (InvalidOperationException) { }
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("mwLazySections.dispose");
        }
        catch (JSDisconnectedException) { }
        catch (InvalidOperationException) { }
        catch (JSException) { }

        _dotNetRef?.Dispose();
        _dotNetRef = null;

        try
        {
            await JS.InvokeVoidAsync("mwDestroyHeroCarousel");
            await JS.InvokeVoidAsync("mwDestroyHomeCarousels");
        }
        catch (JSDisconnectedException) { }
        catch (InvalidOperationException) { }
    }

    protected override Task OnInitializedAsync()
    {
        if (!OperatingSystem.IsBrowser())
            return Task.CompletedTask;

        var cachedProducts = HomeCache.GetHomeProductsCached();
        if (cachedProducts != null)
        {
            ApplyHomeProducts(cachedProducts);
            isLoadingProducts = false;
            _productsRequested = true;
        }

        var cachedCategories = HomeCache.GetCategoriesCached();
        if (cachedCategories != null)
        {
            ApplyCategories(cachedCategories);
            isLoadingCategories = false;
            _categoriesRequested = true;
        }

        // Network fetches wait until sections approach the viewport.
        return Task.CompletedTask;
    }

    private async Task WireLazySectionsAsync()
    {
        if (_lazySectionsWired || !OperatingSystem.IsBrowser())
            return;

        _lazySectionsWired = true;
        _dotNetRef = DotNetObjectReference.Create(this);
        try
        {
            await JS.InvokeVoidAsync("mwLazySections.observe", _dotNetRef, "200px 0px");
        }
        catch (JSDisconnectedException) { }
        catch (InvalidOperationException) { }
        catch (JSException)
        {
            // Fallback if the helper is missing: load immediately.
            await OnSectionVisible("categories");
            await OnSectionVisible("products");
        }
    }

    [JSInvokable]
    public async Task OnSectionVisible(string id)
    {
        if (string.Equals(id, "categories", StringComparison.OrdinalIgnoreCase))
            await EnsureCategoriesAsync();
        else if (string.Equals(id, "products", StringComparison.OrdinalIgnoreCase))
            await EnsureProductsAsync();
    }

    private async Task EnsureCategoriesAsync()
    {
        if (_categoriesRequested || Categories.Count > 0)
            return;

        _categoriesRequested = true;
        await LoadCategoriesAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task EnsureProductsAsync()
    {
        if (_productsRequested
            || NewArrivals.Count > 0
            || FeaturedProducts.Count > 0
            || BestSellerProducts.Count > 0)
            return;

        _productsRequested = true;
        await LoadHomeProductsAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadHomeProductsAsync()
    {
        isLoadingProducts = true;
        try
        {
            var data = await HomeCache.GetHomeProductsAsync(allowStale: false);
            if (data != null)
                ApplyHomeProducts(data);
        }
        catch
        {
        }
        isLoadingProducts = false;
    }

    private async Task LoadCategoriesAsync()
    {
        isLoadingCategories = true;
        try
        {
            var data = await HomeCache.GetCategoriesAsync(allowStale: false);
            if (data != null)
                ApplyCategories(data);
        }
        catch
        {
        }
        isLoadingCategories = false;
    }

    private void ApplyHomeProducts(MuuqWear.Model.Products.HomeProductsModel data)
    {
        NewArrivals = data.NewArrivals;
        FeaturedProducts = data.Featured;
        BestSellerProducts = data.BestSellers;
        _homeCarouselsReady = false;
    }

    private void ApplyCategories(List<MuuqWear.Model.Products.CategoryModel> data)
    {
        Categories = data
            .Select(c => new CategoryCard(
                Name: c.Name!,
                Image: ResolveCategoryImage(c.Name),
                Link: $"/shop/apparel?categoryId={c.Id}"))
            .ToList();
        _homeCarouselsReady = false;
    }

    // Catalog category names are lowercase ("mens"); map must not be case-sensitive.
    public static string ResolveCategoryImage(string? name) =>
        (name ?? "").Trim().ToLowerInvariant() switch
        {
            "mens" => "https://images.unsplash.com/photo-1762232975039-7b36432bcac6?w=600",
            "womens" => "https://images.unsplash.com/photo-1506619928596-bb8c201545cc?w=600",
            "kids" => "https://images.unsplash.com/photo-1759313560190-d160c3567170?w=600",
            "accessories" => "https://images.unsplash.com/photo-1693592401248-c9544518318a?w=600",
            "outerwear" => "https://images.unsplash.com/photo-1704716720991-cf3197cfb190?w=600",
            _ => CategoryImageFallback
        };
}
