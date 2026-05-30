using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MuuqWear.Web.Components.Pages.HomeComponent;

public partial class HomeComponent
{

    //Slider References
    // private ElementReference finishedRef;
    private ElementReference categoryRef;
    private ElementReference newArrivalRef;
    private ElementReference bestSellersRef;
    private ElementReference featuredRef;

    //For Scrolling Manually By Clicking Buttons

    private async Task ScrollRight(ElementReference el)
    {
        await JS.InvokeVoidAsync("mwScrollRight", el, ".mw-categorycarousel-item");
        await JS.InvokeVoidAsync("mwScrollRight", el, ".mw-newarrivalcarousel-item");
        await JS.InvokeVoidAsync("mwScrollRight", el, ".bestsellers-slide");
        await JS.InvokeVoidAsync("mwScrollRight", el, ".mw-featured__card");
    }

    private async Task ScrollLeft(ElementReference el)
    {
        await JS.InvokeVoidAsync("mwScrollLeft", el, ".mw-categorycarousel-item");
        await JS.InvokeVoidAsync("mwScrollLeft", el, ".mw-newarrivalcarousel-item");
        await JS.InvokeVoidAsync("mwScrollLeft", el, ".bestsellers-slide");
        await JS.InvokeVoidAsync("mwScrollLeft", el, ".mw-featured__card");
    }
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


    private record ProductCard(string Name, string Price, string Image, string Link);


    private record BestSellerCard(string Name, string Price, string Image, string Link);



    private List<MuuqWear.Model.Products.ProductModel> NewArrivals = new();
    private List<MuuqWear.Model.Products.ProductModel> FeaturedProducts = new();
    private List<MuuqWear.Model.Products.ProductModel> BestSellerProducts = new();



    // For scrolling by dragging and autosliding images in the Carousel

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {

            // check if recovery token in URL fragment
            var fragment = await JS.InvokeAsync<string>(
                "eval", "window.location.hash");

            if (!string.IsNullOrEmpty(fragment) &&
                fragment.Contains("type=recovery"))
            {
                // redirect to reset password page with fragment
                await JS.InvokeVoidAsync("eval",
                    $"window.location.href = '/auth/reset-password' + window.location.hash");
                return;
            }

            // For AutoSliding Images in carousel
            await JS.InvokeVoidAsync("mwStartAutoSlide", categoryRef, ".mw-categorycarousel-item", 3000);
            await JS.InvokeVoidAsync("mwStartAutoSlide", newArrivalRef, ".mw-newarrivalcarousel-item", 3000);
            await JS.InvokeVoidAsync("mwStartAutoSlide", bestSellersRef, ".bestsellers-slide", 3000);
            await JS.InvokeVoidAsync("mwStartAutoSlide", featuredRef, ".mw-featured__card", 3000);

            //For Sliding Images by dragging and scrolling
            await JS.InvokeVoidAsync("enableDragScroll", categoryRef);
            await JS.InvokeVoidAsync("enableDragScroll", newArrivalRef);
            await JS.InvokeVoidAsync("enableDragScroll", bestSellersRef);
            await JS.InvokeVoidAsync("enableDragScroll", featuredRef);

        }
    }

    protected override async Task OnInitializedAsync()
    {
        var result = await ProductService.GetHomeProducts();

        if (result.Success && result.Data != null)
        {
            NewArrivals = result.Data.NewArrivals;
            FeaturedProducts = result.Data.Featured;
            BestSellerProducts = result.Data.BestSellers;
        }

        await LoadCategories();

    }
    private async Task LoadCategories()
    {
        var result = await CategoryService.GetAll();

        if (result.Success && result.Data != null)
        {
            Categories = result.Data
                .Select(c => new CategoryCard(
                    Name: c.Name!,
                    Image: CategoryImages!.GetValueOrDefault(c.Name,
                        "https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=600"),
                    Link: $"/shop/apparel?categoryId={c.Id}" // FIXED
                ))
                .ToList();
        }
    }

}
