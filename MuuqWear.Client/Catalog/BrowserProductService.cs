using MuuqWear.Application.Catalog;
using MuuqWear.Application.Services.ProductService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Products;
using MuuqWear.Model.Shared;

namespace MuuqWear.Client.Catalog;

public sealed class BrowserProductService : IProductService
{
    private readonly BrowserCatalog _catalog;

    public BrowserProductService(BrowserCatalog catalog) => _catalog = catalog;

    public async Task<Response<PaginatedResponse<ProductModel>>> GetAll(ProductFilterModel filter)
    {
        var all = await _catalog.ProductsAsync();
        IEnumerable<ProductModel> query = all;
        if (filter.CategoryId is Guid categoryId && categoryId != Guid.Empty)
            query = query.Where(p => p.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(p => (p.Name ?? "").Contains(filter.Search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter.Sizes))
        {
            var wanted = filter.Sizes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            query = query.Where(p => p.SizeStock.Any(s => wanted.Contains(s.Size, StringComparer.OrdinalIgnoreCase)));
        }
        if (filter.MinPrice is > 0)
            query = query.Where(p => p.Price >= filter.MinPrice);
        if (filter.MaxPrice is > 0)
            query = query.Where(p => p.Price <= filter.MaxPrice);
        query = filter.SortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderBy(p => p.Name)
        };
        return Response<PaginatedResponse<ProductModel>>.SuccessResponse(
            StorefrontCatalogMap.Page(query.ToList(), filter.Page, filter.PageSize));
    }

    public async Task<Response<HomeProductsModel>> GetHomeProducts()
    {
        var all = await _catalog.ProductsAsync();
        return Response<HomeProductsModel>.SuccessResponse(new HomeProductsModel
        {
            NewArrivals = TakeOrFill(all, p => p.IsNewArrival, 6),
            Featured = TakeOrFill(all, p => p.IsFeatured, 6),
            BestSellers = TakeOrFill(all, p => p.IsBestSeller, 6)
        });
    }

    public async Task<Response<ProductModel>> GetById(Guid id)
    {
        var product = await _catalog.ProductAsync(id.ToString());
        return product is null
            ? Response<ProductModel>.Fail("Product not found")
            : Response<ProductModel>.SuccessResponse(product);
    }

    public async Task<Response<List<ProductModel>>> GetRelated(Guid id)
    {
        var current = await _catalog.ProductAsync(id.ToString());
        var all = await _catalog.ProductsAsync();
        var related = all.Where(p => p.Id != current?.Id && p.CategoryId == current?.CategoryId).Take(4).ToList();
        return Response<List<ProductModel>>.SuccessResponse(related);
    }

    public async Task<Response<List<SizeStockModel>>> GetSizeStock(Guid productId)
    {
        var product = await _catalog.ProductAsync(productId.ToString());
        return product is null
            ? Response<List<SizeStockModel>>.Fail("Product not found")
            : Response<List<SizeStockModel>>.SuccessResponse(product.SizeStock);
    }

    public Task<Response<ProductModel>> Add(AddProductModel request) => AdminOnly<ProductModel>();
    public Task<Response<string>> UploadImage(Stream fileStream, string fileName, string contentType) => AdminOnly<string>();
    public Task<Response<ProductModel>> Update(Guid id, UpdateProductModel request) => AdminOnly<ProductModel>();
    public Task<Response<bool>> Delete(Guid id) => AdminOnly<bool>();
    public Task<Response<ProductImageModel>> AddProductImage(AddProductImageModel request) => AdminOnly<ProductImageModel>();
    public Task<Response<bool>> DeleteProductImage(Guid imageId) => AdminOnly<bool>();
    public Task<Response<SizeStockModel>> UpdateSizeStock(Guid sizeStockId, int quantity) => AdminOnly<SizeStockModel>();
    public Task<Response<ProductModel>> UpdateStock(Guid productId, int totalStock) => AdminOnly<ProductModel>();
    public Task<Response<SizeStockModel>> AddSizeStock(Guid productId, string size, int quantity) => AdminOnly<SizeStockModel>();
    public Task<Response<BatchUpdateSizeStockResult>> UpdateSizeStockBatch(Guid productId, BatchUpdateSizeStockRequest request) =>
        AdminOnly<BatchUpdateSizeStockResult>();
    public Task<Response<bool>> DeleteSizeStock(Guid sizeStockId) => AdminOnly<bool>();

    private static Task<Response<T>> AdminOnly<T>() =>
        Task.FromResult(Response<T>.Fail("Staff tools stay on the admin host."));

    private static List<ProductModel> TakeOrFill(List<ProductModel> all, Func<ProductModel, bool> match, int take)
    {
        var hit = all.Where(match).Take(take).ToList();
        if (hit.Count >= take)
            return hit;
        hit.AddRange(all.Where(p => hit.All(x => x.Id != p.Id)).Take(take - hit.Count));
        return hit;
    }
}
