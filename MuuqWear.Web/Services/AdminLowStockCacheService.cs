using MuuqWear.Application.Services.ProductService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Products;

namespace MuuqWear.Web.Services;

/// <summary>
/// Caches low-stock product snapshots for admin nav badges and dashboard.
/// Avoids refetching up to 1000 products on every admin navigation.
/// </summary>
public sealed class AdminLowStockCacheService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    private readonly IProductService _productService;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private IReadOnlyList<ProductModel>? _lowStockProducts;
    private DateTime _cachedAt;

    public AdminLowStockCacheService(IProductService productService)
    {
        _productService = productService;
    }

    public void Invalidate() => _lowStockProducts = null;

    public async Task<int> GetCountAsync(bool forceRefresh = false)
    {
        var snapshot = await GetSnapshotAsync(forceRefresh);
        return snapshot.Count;
    }

    public async Task<IReadOnlyList<ProductModel>> GetLowStockProductsAsync(bool forceRefresh = false)
    {
        var snapshot = await GetSnapshotAsync(forceRefresh);
        return snapshot.Products;
    }

    private async Task<(int Count, IReadOnlyList<ProductModel> Products)> GetSnapshotAsync(bool forceRefresh)
    {
        if (!forceRefresh && _lowStockProducts != null &&
            DateTime.UtcNow - _cachedAt < CacheDuration)
        {
            return (_lowStockProducts.Count, _lowStockProducts);
        }

        await _gate.WaitAsync();
        try
        {
            if (!forceRefresh && _lowStockProducts != null &&
                DateTime.UtcNow - _cachedAt < CacheDuration)
            {
                return (_lowStockProducts.Count, _lowStockProducts);
            }

            var result = await _productService.GetAll(new ProductFilterModel
            {
                Page = 1,
                PageSize = 1000
            });

            if (!result.Success || result.Data?.Data == null)
            {
                _lowStockProducts = Array.Empty<ProductModel>();
                _cachedAt = DateTime.UtcNow;
                return (0, _lowStockProducts);
            }

            foreach (var product in result.Data.Data)
                ProductStockHelper.SyncStockFromSizes(product);

            _lowStockProducts = result.Data.Data
                .Where(ProductStockHelper.IsLowStock)
                .OrderBy(p => ProductStockHelper.GetEffectiveStock(p))
                .ThenBy(p => p.Name)
                .ToList();
            _cachedAt = DateTime.UtcNow;

            return (_lowStockProducts.Count, _lowStockProducts);
        }
        finally
        {
            _gate.Release();
        }
    }
}
