using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MuuqWear.Web.Components.Pages
{
    public partial class Home
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
        private record CategoryCard(string Name, string Image, string Link);

        private List<CategoryCard> Categories = new()
    {
    new("Men", "https://images.unsplash.com/photo-1762232975039-7b36432bcac6?w=600", "/shop"),
    new("Women", "https://images.unsplash.com/photo-1506619928596-bb8c201545cc?w=600", "/shop"),
    new("Kids", "https://images.unsplash.com/photo-1759313560190-d160c3567170?w=600", "/shop"),
    new("Accessories", "https://images.unsplash.com/photo-1693592401248-c9544518318a?w=600", "/shop"),
    new("Outerwear", "https://images.unsplash.com/photo-1704716720991-cf3197cfb190?w=600", "/shop")
    };

        private record ProductCard(string Name, string Price, string Image, string Link);

        private List<ProductCard> Products = new()
    {
    new("City Commuter Pack", "$95.00", "https://images.unsplash.com/photo-1770385436782-e47efd8d1e80?w=400&h=530&fit=crop", "/product"),
    new("Aero Tech Jacket", "$145.00", "https://images.unsplash.com/photo-1654182603195-b7bee5d08270?w=400&h=530&fit=crop", "/product"),
    new("Lunar Knit Sweater", "$95.00", "https://images.unsplash.com/photo-1642853474532-9aca78f70629?w=400&h=530&fit=crop", "/product"),
    new("Apex Runners", "$160.00", "https://images.unsplash.com/photo-1718802323158-b32c0330ad4a?w=400&h=530&fit=crop", "/product"),
    new("Nova Canvas Tote","$55.00","https://images.unsplash.com/photo-1693592401248-c9544518318a?w=400&amph=530&ampfit=crop","/product"),
    new("Field Utility Jacket","$175.00","https://images.unsplash.com/photo-1620231151282-957594b30e94?w=400&amph=530&ampfit=crop","/product")

    };

        private record FeaturedCard(string Title, string Description, string Image, string Link);


        private List<FeaturedCard> Featured = new()
    {
    new("Minimalist Knitwear",
    "Refined textures for the discerning wardrobe. Essential pieces for effortless layering.",
    "https://images.unsplash.com/photo-1642853474532-9aca78f70629?w=600",
    "/shop"),

    new("Urban Explorer",
    "City-proof essentials built for all-day comfort. From dawn commute to evening out.",
    "https://images.unsplash.com/photo-1762232975039-7b36432bcac6?w=600",
    "/shop"),

    new("The Technical Layer",
    "Advanced materials meets urban utility. Discover performance-ready outerwear.",
    "https://images.unsplash.com/photo-1620231151282-957594b30e94?w=600",
    "/shop"),
    };

        private record BestSellerCard(string Name, string Price, string Image, string Link);

        private List<BestSellerCard> BestSellers = new()
    {
    new("Classic Navy Tee", "$45.00", "https://images.unsplash.com/photo-1618840198256-abe11bda33dc?w=400&amph=530&ampfit=crop", "/productdetail"),
    new("Sapphire Hoodie", "$85.00", "https://images.unsplash.com/photo-1722620197153-eb2549909098?w=400&amph=530&ampfit=crop", "/productdetail"),
    new("Azure Chinos", "$70.00", "https://images.unsplash.com/photo-1768696081520-76c372aa0d00?w=400&amph=530&ampfit=crop", "/productdetail"),
    new("Cobalt Jacket", "$120.00", "https://images.unsplash.com/photo-1538516089546-40b08c9c9866?w=400&amph=530&ampfit=crop", "/productdetail"),
    new("Selvedge Denim","$120.00","https://images.unsplash.com/photo-1555689502-c4b22d76c56f?w=400&amph=530&ampfit=crop","/productdetail"),
    new("Court Sneakers","$175.00","https://images.unsplash.com/photo-1718802323158-b32c0330ad4a?w=400&amph=530&ampfit=crop","/productdetail")

    };


        private List<MuuqWear.Model.Products.ProductModel> NewArrivals = new();
        private List<MuuqWear.Model.Products.ProductModel> FeaturedProducts = new();
        private List<MuuqWear.Model.Products.ProductModel> BestSellerProducts = new();



        // For scrolling by dragging and autosliding images in the Carousel

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {

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
            var result = await ProductService.GetAll();

            if (result.Success && result.Data != null)
            {
                NewArrivals = result.Data.Where(p => p.IsNewArrival && p.IsActive).ToList();
                FeaturedProducts = result.Data.Where(p => p.IsFeatured && p.IsActive).ToList();
                BestSellerProducts = result.Data.Where(p => p.IsBestSeller && p.IsActive).ToList();
            }
        }

    }
}