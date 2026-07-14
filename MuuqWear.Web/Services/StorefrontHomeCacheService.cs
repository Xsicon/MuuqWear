using MuuqWear.Application.Services.CategoryService;
using MuuqWear.Application.Services.ProductService;
using MuuqWear.Model.Products;

namespace MuuqWear.Web.Services;

public sealed class StorefrontHomeCacheService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private HomeProductsModel? _homeProducts;
    private List<CategoryModel>? _categories;
    private DateTime _homeCachedAt;
    private DateTime _categoriesCachedAt;

    public StorefrontHomeCacheService(IProductService productService, ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    public bool HasHomeProducts => _homeProducts != null;
    public bool HasCategories => _categories != null;

    public HomeProductsModel? GetHomeProductsCached() => _homeProducts;
    public List<CategoryModel>? GetCategoriesCached() => _categories;

    public async Task<HomeProductsModel?> GetHomeProductsAsync(bool allowStale = true)
    {
        if (_homeProducts != null && IsFresh(_homeCachedAt))
            return _homeProducts;

        if (_homeProducts != null && allowStale)
        {
            _ = RefreshHomeInBackgroundAsync();
            return _homeProducts;
        }

        await RefreshHomeAsync();
        return _homeProducts;
    }

    public async Task<List<CategoryModel>?> GetCategoriesAsync(bool allowStale = true)
    {
        if (_categories != null && IsFresh(_categoriesCachedAt))
            return _categories;

        if (_categories != null && allowStale)
        {
            _ = RefreshCategoriesInBackgroundAsync();
            return _categories;
        }

        await RefreshCategoriesAsync();
        return _categories;
    }

    private async Task RefreshHomeInBackgroundAsync()
    {
        try
        {
            await RefreshHomeAsync();
        }
        catch
        {
            // Keep stale data on background refresh failure
        }
    }

    private async Task RefreshCategoriesInBackgroundAsync()
    {
        try
        {
            await RefreshCategoriesAsync();
        }
        catch
        {
            // Keep stale data on background refresh failure
        }
    }

    private async Task RefreshHomeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var result = await _productService.GetHomeProducts();
            if (result.Success && result.Data != null)
            {
                _homeProducts = result.Data;
                _homeCachedAt = DateTime.UtcNow;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task RefreshCategoriesAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var result = await _categoryService.GetAll();
            if (result.Success && result.Data != null)
            {
                _categories = result.Data;
                _categoriesCachedAt = DateTime.UtcNow;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private static bool IsFresh(DateTime cachedAt) =>
        cachedAt != default && DateTime.UtcNow - cachedAt < CacheDuration;
}
