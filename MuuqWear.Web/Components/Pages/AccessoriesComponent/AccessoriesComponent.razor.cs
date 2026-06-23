using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MuuqWear.Application.Services.CartService;
using MuuqWear.Application.Services.CategoryService;
using MuuqWear.Application.Services.ProductService;
using MuuqWear.Model.Cart;
using MuuqWear.Model.Products;

namespace MuuqWear.Web.Components.Pages.AccessoriesComponent;

public partial class AccessoriesComponent : IAsyncDisposable
{
    [Inject] private IProductService ProductService { get; set; } = default!;
    [Inject] private ICategoryService CategoryService { get; set; } = default!;
    [Inject] private CartStateService CartStateService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ShopFilterModel filter = new();
    private List<ProductModel> allProducts = new();
    private Guid? accessoriesCategoryId;
    private bool isLoading = true;
    private bool isInitError;
    private string selectedSubcategory = "All";

    private ProductModel? selectedProduct;
    private string selectedColor = string.Empty;
    private bool isAddingToCart;

    private ElementReference featuredRef;
    private ElementReference bestSellersRef;
    private ElementReference newArrivalsRef;
    private ElementReference outfitRef;
    private bool _carouselsReady;

    private static readonly string[] SubcategoryFilters =
    {
        "All", "Bags", "Footwear", "Accessories"
    };

    private static readonly string[] ModalFeatures =
    {
        "Premium materials and construction",
        "Minimalist design aesthetic",
        "Lifetime warranty included",
        "Free shipping on orders over $100"
    };

    private IEnumerable<ProductModel> FilteredProducts =>
        selectedSubcategory == "All"
            ? allProducts
            : allProducts.Where(p => string.Equals(
                GetProductSubcategory(p),
                selectedSubcategory,
                StringComparison.OrdinalIgnoreCase));

    private IEnumerable<ProductModel> FeaturedProducts =>
        PickProducts(p => p.IsFeatured || IsBadge(p, "Featured"), 5);

    private IEnumerable<ProductModel> BestSellerProducts =>
        PickProducts(p => p.IsBestSeller || IsBadge(p, "Best Seller"), 6);

    private IEnumerable<ProductModel> NewArrivalProducts =>
        PickProducts(p => p.IsNewArrival || IsBadge(p, "New"), 5);

    private IEnumerable<OutfitPairing> OutfitPairings => OutfitPairingData;

    protected override async Task OnInitializedAsync()
    {
        var categoryResult = await CategoryService.GetAll();
        if (!categoryResult.Success || categoryResult.Data == null)
        {
            isInitError = true;
            isLoading = false;
            return;
        }

        var accessories = categoryResult.Data
            .FirstOrDefault(c => c.Name!.Equals("Accessories", StringComparison.OrdinalIgnoreCase));

        if (accessories == null)
        {
            isInitError = true;
            isLoading = false;
            return;
        }

        accessoriesCategoryId = accessories.Id;
        filter.CategoryId = accessoriesCategoryId;
        filter.PageSize = 100;

        await LoadProducts();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (isLoading || _carouselsReady)
            return;

        if (!isInitError && allProducts.Count > 0)
        {
            await InitCarousel(featuredRef);
            await InitCarousel(bestSellersRef);
            await InitCarousel(newArrivalsRef);
        }

        await InitCarousel(outfitRef);
        _carouselsReady = true;
    }

    private Task InitCarousel(ElementReference trackRef) =>
        JS.InvokeVoidAsync("mwInitAccessoryCarousel", trackRef, ".acc-carousel-item").AsTask();

    private async Task LoadProducts()
    {
        isLoading = true;

        var result = await ProductService.GetAll(filter.ToFilterModel());

        if (result.Success && result.Data != null)
        {
            allProducts = result.Data.Data;
            filter.Page = result.Data.Page;
        }

        isLoading = false;
    }

    private IEnumerable<ProductModel> PickProducts(
        Func<ProductModel, bool> predicate,
        int take)
    {
        var matched = FilteredProducts.Where(predicate).Take(take).ToList();
        if (matched.Count > 0)
            return matched;

        return FilteredProducts.Take(take);
    }

    private static bool IsBadge(ProductModel product, string badge) =>
        string.Equals(product.Badge, badge, StringComparison.OrdinalIgnoreCase);

    private static string GetProductSubcategory(ProductModel product) =>
        string.IsNullOrWhiteSpace(product.Category)
            ? "Accessories"
            : product.Category!;

    private static string GetBadgeClass(string? badge) =>
        badge switch
        {
            "New" => "acc-card__badge--new",
            "Best Seller" => "acc-card__badge--bestseller",
            _ => "acc-card__badge--default"
        };

    private void SelectSubcategory(string subcategory)
    {
        selectedSubcategory = subcategory;
        _carouselsReady = false;
    }

    private void OpenProduct(ProductModel product)
    {
        selectedProduct = product;
        selectedColor = product.ColorOptions.FirstOrDefault() ?? string.Empty;
    }

    private void CloseProduct() =>
        selectedProduct = null;

    private void SelectColor(string color) =>
        selectedColor = color;

    private async Task HandleAddToCart()
    {
        if (selectedProduct == null)
            return;

        if (selectedProduct.ColorOptions.Any() && string.IsNullOrWhiteSpace(selectedColor))
            return;

        isAddingToCart = true;
        StateHasChanged();

        var defaultSize = selectedProduct.SizeStock.FirstOrDefault()?.Size ?? "One Size";

        var success = await CartStateService.AddItem(new AddCartItemModel
        {
            ProductId = selectedProduct.Id,
            Size = defaultSize,
            Quantity = 1,
            ProductName = selectedProduct.Name,
            ProductImageUrl = selectedProduct.ImageUrl,
            ProductPrice = selectedProduct.Price,
            Color = selectedColor
        });

        isAddingToCart = false;

        if (success)
            CloseProduct();

        StateHasChanged();
    }

    private Task ScrollLeft(ElementReference el) =>
        JS.InvokeVoidAsync("mwScrollLeft", el, ".acc-carousel-item").AsTask();

    private Task ScrollRight(ElementReference el) =>
        JS.InvokeVoidAsync("mwScrollRight", el, ".acc-carousel-item").AsTask();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private sealed record OutfitPairing(
        int Id,
        string Title,
        string Description,
        string ImageUrl,
        string PairsWith,
        string[] Tags);

    private static readonly OutfitPairing[] OutfitPairingData =
    {
        new(1, "The Urban Edit",
            "Crossbody bag meets tailored trousers and a structured coat — city-ready from morning to evening.",
            "https://images.unsplash.com/photo-1490481651871-ab68de25d43d?w=600&h=900&fit=crop",
            "Veil Crossbody Bag", ["Smart Casual", "City"]),
        new(2, "Monochrome Moment",
            "Head-to-toe navy anchored by the Sapphire Beanie and matching leather accessories.",
            "https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?w=600&h=900&fit=crop",
            "Sapphire Beanie", ["Monochrome", "Winter"]),
        new(3, "Clean Minimalism",
            "M-01 Court Sneakers ground a white relaxed shirt and straight-cut trousers — effortless and precise.",
            "https://images.unsplash.com/photo-1509631179647-0177331693ae?w=600&h=900&fit=crop",
            "M-01 Court Sneaker", ["Minimalist", "Everyday"]),
        new(4, "Weekend Wanderer",
            "City Commuter 20L with relaxed linen separates and leather sandals for unhurried Saturdays.",
            "https://images.unsplash.com/photo-1551488831-00ddcb6c6bd3?w=600&h=900&fit=crop",
            "City Commuter 20L", ["Casual", "Weekend"]),
        new(5, "The Considered Layers",
            "Midnight Wrap Scarf adds warmth and texture over a camel overcoat and slim dark denim.",
            "https://images.unsplash.com/photo-1539109136881-3be0616acf4b?w=600&h=900&fit=crop",
            "Midnight Wrap Scarf", ["Layering", "Autumn"]),
        new(6, "Precision Dressing",
            "Minimalist Belt cinches a wide-leg trouser and fine-knit turtleneck into architectural proportion.",
            "https://images.unsplash.com/photo-1469334031218-e382a71b716b?w=600&h=900&fit=crop",
            "Minimalist Belt", ["Tailored", "Editorial"])
    };
}
