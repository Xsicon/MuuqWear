using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MuuqWear.Application.Services.JobPostingService;
namespace MuuqWear.Web.Components.Pages.HomeComponent;

public partial class HomeComponent : IAsyncDisposable
{
    private const int HeroAutoplayMs = 5500;

    private record HeroSlide(string Image, string Eyebrow, string Headline, string Cta, string Link);

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

    private record CategoryCard(string Name, string Image, string Link);

    private Dictionary<string, string> CategoryImages = new()
    {
        { "Mens", "https://images.unsplash.com/photo-1762232975039-7b36432bcac6?w=600" },
        { "Womens", "https://images.unsplash.com/photo-1506619928596-bb8c201545cc?w=600" },
        { "Kids", "https://images.unsplash.com/photo-1759313560190-d160c3567170?w=600" },
        { "Accessories", "https://images.unsplash.com/photo-1693592401248-c9544518318a?w=600" },
        { "Outerwear", "https://images.unsplash.com/photo-1704716720991-cf3197cfb190?w=600" }
    };

    private List<MuuqWear.Model.Products.ProductModel> NewArrivals = new();
    private List<MuuqWear.Model.Products.ProductModel> FeaturedProducts = new();
    private List<MuuqWear.Model.Products.ProductModel> BestSellerProducts = new();
    private bool _homeCarouselsReady;

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
        await Task.WhenAll(LoadHomeProducts(), LoadCategories());
    }

    private async Task LoadHomeProducts()
    {
        var result = await ProductService.GetHomeProducts();

        if (result.Success && result.Data != null)
        {
            NewArrivals = result.Data.NewArrivals;
            FeaturedProducts = result.Data.Featured;
            BestSellerProducts = result.Data.BestSellers;
        }
    }

    private async Task LoadCategories()
    {
        var result = await CategoryService.GetAll();

        if (result.Success && result.Data != null)
        {
            Categories = result.Data
                .Select(c => new CategoryCard(
                    Name: c.Name!,
                    Image: CategoryImages.GetValueOrDefault(c.Name!,
                        "https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=600"),
                    Link: $"/shop/apparel?categoryId={c.Id}"
                ))
                .ToList();
        }
    }
}
