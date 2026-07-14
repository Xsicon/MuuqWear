using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MuuqWear.Web.Services;

namespace MuuqWear.Web.Components.Pages.HomeComponent;

public partial class HomeComponent : IAsyncDisposable
{
    private const int HeroAutoplayMs = 5500;

    private record HeroSlide(string Image, string Eyebrow, string Headline, string Cta, string Link);

    private record CategoryCard(string Name, string Image, string Link);

    [Inject] private StorefrontHomeCacheService HomeCache { get; set; } = default!;

    private List<HeroSlide> HeroSlides { get; } = new()
    {
        new(
            "https://images.unsplash.com/photo-1756361771567-e276865b77cb?w=1400&h=700&fit=crop",
            "Limited Release",
            "The Sapphire Veil Collection",
            "Explore the Collection",
            "/shop/apparel"),
        new(
            "https://images.unsplash.com/photo-1558769132-cb1aea458c5e?w=1600&h=900&fit=crop",
            "New Arrivals",
            "Modern Modest Essentials",
            "Shop Now",
            "/shop/apparel"),
        new(
            "https://images.unsplash.com/photo-1761957375235-46acb4862151?w=1400&h=700&fit=crop",
            "Limited Release",
            "Built by Community",
            "Discover More",
            "/shop/accessories")
    };

    private List<CategoryCard> Categories = new();
    private List<MuuqWear.Model.Products.ProductModel> NewArrivals = new();
    private List<MuuqWear.Model.Products.ProductModel> FeaturedProducts = new();
    private List<MuuqWear.Model.Products.ProductModel> BestSellerProducts = new();

    private bool _homeCarouselsReady;
    private bool isLoadingProducts = true;
    private bool isLoadingCategories = true;

    private Dictionary<string, string> CategoryImages { get; } = new()
    {
        { "Mens", "https://images.unsplash.com/photo-1762232975039-7b36432bcac6?w=600" },
        { "Womens", "https://images.unsplash.com/photo-1506619928596-bb8c201545cc?w=600" },
        { "Kids", "https://images.unsplash.com/photo-1759313560190-d160c3567170?w=600" },
        { "Accessories", "https://images.unsplash.com/photo-1693592401248-c9544518318a?w=600" },
        { "Outerwear", "https://images.unsplash.com/photo-1704716720991-cf3197cfb190?w=600" }
    };

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
            await JS.InvokeVoidAsync("mwDestroyHeroCarousel");
            await JS.InvokeVoidAsync("mwDestroyHomeCarousels");
        }
        catch (JSDisconnectedException) { }
        catch (InvalidOperationException) { }
    }

    protected override async Task OnInitializedAsync()
    {
        var cachedProducts = HomeCache.GetHomeProductsCached();
        if (cachedProducts != null)
        {
            ApplyHomeProducts(cachedProducts);
            isLoadingProducts = false;
        }

        var cachedCategories = HomeCache.GetCategoriesCached();
        if (cachedCategories != null)
        {
            ApplyCategories(cachedCategories);
            isLoadingCategories = false;
        }

        var tasks = new List<Task>();

        if (isLoadingProducts)
            tasks.Add(LoadHomeProductsAsync());

        if (isLoadingCategories)
            tasks.Add(LoadCategoriesAsync());

        if (tasks.Count > 0)
            await Task.WhenAll(tasks);
    }

    private async Task LoadHomeProductsAsync()
    {
        isLoadingProducts = true;
        var data = await HomeCache.GetHomeProductsAsync(allowStale: false);
        if (data != null)
            ApplyHomeProducts(data);
        isLoadingProducts = false;
    }

    private async Task LoadCategoriesAsync()
    {
        isLoadingCategories = true;
        var data = await HomeCache.GetCategoriesAsync(allowStale: false);
        if (data != null)
            ApplyCategories(data);
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
                Image: CategoryImages.GetValueOrDefault(c.Name!,
                    "https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=600"),
                Link: $"/shop/apparel?categoryId={c.Id}"))
            .ToList();
        _homeCarouselsReady = false;
    }
}
