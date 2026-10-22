using Google.Cloud.Storage.V1;
using MuuqWear.Application.Catalog;
using MuuqWear.Application.Services.ProductService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Products;
using MuuqWear.Model.Shared;

namespace MuuqWear.Web.Services;

public sealed class FirestoreProductService : IProductService
{
    private readonly AdminFirestore _store;
    private readonly IConfiguration _config;

    public FirestoreProductService(AdminFirestore store, IConfiguration config)
    {
        _store = store;
        _config = config;
    }

    public async Task<Response<PaginatedResponse<ProductModel>>> GetAll(ProductFilterModel filter)
    {
        var all = await _store.ListProductsAsync();
        IEnumerable<ProductModel> query = all;
        if (!filter.IncludeTickets)
            query = query.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(p => (p.Name ?? "").Contains(filter.Search, StringComparison.OrdinalIgnoreCase));
        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId);
        var list = query.ToList();
        return Response<PaginatedResponse<ProductModel>>.SuccessResponse(
            StorefrontCatalogMap.Page(list, filter.Page, filter.PageSize));
    }

    public async Task<Response<HomeProductsModel>> GetHomeProducts()
    {
        var all = (await _store.ListProductsAsync()).Where(p => p.IsActive).ToList();
        return Response<HomeProductsModel>.SuccessResponse(new HomeProductsModel
        {
            NewArrivals = all.Where(p => p.IsNewArrival).Take(6).ToList(),
            Featured = all.Where(p => p.IsFeatured).Take(6).ToList(),
            BestSellers = all.Where(p => p.IsBestSeller).Take(6).ToList()
        });
    }

    public async Task<Response<ProductModel>> Add(AddProductModel request)
    {
        var product = new ProductModel
        {
            Name = request.Name,
            Price = request.Price,
            ImageUrl = request.ImageUrl,
            IsActive = request.IsActive,
            IsNewArrival = request.IsNewArrival,
            IsFeatured = request.IsFeatured,
            IsBestSeller = request.IsBestSeller,
            Description = request.Description,
            CategoryId = request.CategoryId,
            Category = request.Category,
            ColorOptions = request.ColorOptions,
            Images = request.Images,
            SizeStock = request.Sizes.Select(s => new SizeStockModel { Size = s, Quantity = request.Stock }).ToList(),
            CreatedAt = DateTime.UtcNow
        };
        return Response<ProductModel>.SuccessResponse(await _store.UpsertProductAsync(product));
    }

    public async Task<Response<string>> UploadImage(Stream fileStream, string fileName, string contentType)
    {
        var bucket = _config["Firebase:StorageBucket"] ?? "muuqwear-6e2e0.firebasestorage.app";
        var objectName = $"products/{Guid.NewGuid():N}/{Path.GetFileName(fileName)}";
        var client = await StorageClient.CreateAsync();
        await client.UploadObjectAsync(bucket, objectName, contentType, fileStream);
        return Response<string>.SuccessResponse(StorefrontCatalogMap.ImageUrl(objectName));
    }

    public async Task<Response<ProductModel>> Update(Guid id, UpdateProductModel request)
    {
        var existing = await _store.GetProductAsync(id);
        if (existing is null)
            return Response<ProductModel>.Fail("Product not found");
        existing.Name = request.Name ?? existing.Name;
        existing.Price = request.Price;
        existing.ImageUrl = request.ImageUrl ?? existing.ImageUrl;
        existing.IsActive = request.IsActive;
        existing.IsNewArrival = request.IsNewArrival;
        existing.IsFeatured = request.IsFeatured;
        existing.IsBestSeller = request.IsBestSeller;
        existing.Description = request.Description ?? existing.Description;
        existing.CategoryId = request.CategoryId ?? existing.CategoryId;
        existing.Category = request.Category ?? existing.Category;
        existing.ColorOptions = request.ColorOptions.Count == 0 ? existing.ColorOptions : request.ColorOptions;
        return Response<ProductModel>.SuccessResponse(await _store.UpsertProductAsync(existing));
    }

    public async Task<Response<bool>> Delete(Guid id)
    {
        await _store.DeleteProductAsync(id);
        return Response<bool>.SuccessResponse(true);
    }

    public async Task<Response<ProductModel>> GetById(Guid id)
    {
        var product = await _store.GetProductAsync(id);
        return product is null ? Response<ProductModel>.Fail("Product not found") : Response<ProductModel>.SuccessResponse(product);
    }

    public async Task<Response<List<ProductModel>>> GetRelated(Guid id)
    {
        var current = await _store.GetProductAsync(id);
        var all = await _store.ListProductsAsync();
        return Response<List<ProductModel>>.SuccessResponse(
            all.Where(p => p.Id != id && p.IsActive && p.CategoryId == current?.CategoryId).Take(4).ToList());
    }

    public async Task<Response<ProductImageModel>> AddProductImage(AddProductImageModel request)
    {
        var product = await _store.GetProductAsync(request.ProductId);
        if (product is null)
            return Response<ProductImageModel>.Fail("Product not found");
        var image = new ProductImageModel
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            ImageUrl = request.ImageUrl,
            SortOrder = product.Images.Count
        };
        product.Images.Add(image);
        await _store.UpsertProductAsync(product);
        return Response<ProductImageModel>.SuccessResponse(image);
    }

    public async Task<Response<bool>> DeleteProductImage(Guid imageId)
    {
        foreach (var product in await _store.ListProductsAsync())
        {
            var image = product.Images.FirstOrDefault(i => i.Id == imageId);
            if (image is null)
                continue;
            product.Images.Remove(image);
            await _store.UpsertProductAsync(product);
            return Response<bool>.SuccessResponse(true);
        }
        return Response<bool>.Fail("Image not found");
    }

    public async Task<Response<SizeStockModel>> UpdateSizeStock(Guid sizeStockId, int quantity)
    {
        var hit = await _store.FindSizeAsync(sizeStockId);
        if (hit is null)
            return Response<SizeStockModel>.Fail("Size not found");
        var map = hit.Value.Product.SizeStock.ToDictionary(s => s.Size, s => s.Id == sizeStockId ? quantity : s.Quantity);
        await _store.UpdateSizeStockAsync(hit.Value.Product.Id, map);
        hit.Value.Size.Quantity = quantity;
        return Response<SizeStockModel>.SuccessResponse(hit.Value.Size);
    }

    public async Task<Response<ProductModel>> UpdateStock(Guid productId, int totalStock)
    {
        var product = await _store.GetProductAsync(productId);
        if (product is null)
            return Response<ProductModel>.Fail("Product not found");
        if (product.SizeStock.Count == 0)
            product.SizeStock.Add(new SizeStockModel { Size = "OS", Quantity = totalStock });
        else
            product.SizeStock[0].Quantity = totalStock;
        return Response<ProductModel>.SuccessResponse(await _store.UpsertProductAsync(product));
    }

    public async Task<Response<List<SizeStockModel>>> GetSizeStock(Guid productId) =>
        Response<List<SizeStockModel>>.SuccessResponse(await _store.GetSizeStockAsync(productId));

    public async Task<Response<SizeStockModel>> AddSizeStock(Guid productId, string size, int quantity)
    {
        var product = await _store.GetProductAsync(productId);
        if (product is null)
            return Response<SizeStockModel>.Fail("Product not found");
        product.SizeStock.Add(new SizeStockModel { Size = size, Quantity = quantity });
        await _store.UpsertProductAsync(product);
        var saved = (await _store.GetProductAsync(productId))!.SizeStock.First(s => s.Size == size);
        return Response<SizeStockModel>.SuccessResponse(saved);
    }

    public async Task<Response<BatchUpdateSizeStockResult>> UpdateSizeStockBatch(Guid productId, BatchUpdateSizeStockRequest request)
    {
        var product = await _store.GetProductAsync(productId);
        if (product is null)
            return Response<BatchUpdateSizeStockResult>.Fail("Product not found");
        foreach (var item in request.Items)
        {
            var size = product.SizeStock.FirstOrDefault(s => s.Id == item.SizeStockId);
            if (size is not null)
                size.Quantity = item.Quantity;
        }
        foreach (var upsert in request.Upserts)
        {
            var size = product.SizeStock.FirstOrDefault(s => string.Equals(s.Size, upsert.Size, StringComparison.OrdinalIgnoreCase));
            if (size is null)
                product.SizeStock.Add(new SizeStockModel { Size = upsert.Size, Quantity = upsert.Quantity });
            else
                size.Quantity = upsert.Quantity;
        }
        var saved = await _store.UpsertProductAsync(product);
        return Response<BatchUpdateSizeStockResult>.SuccessResponse(new BatchUpdateSizeStockResult
        {
            SizeStock = saved.SizeStock,
            TotalStock = saved.SizeStock.Sum(s => s.Quantity)
        });
    }

    public async Task<Response<bool>> DeleteSizeStock(Guid sizeStockId)
    {
        var hit = await _store.FindSizeAsync(sizeStockId);
        if (hit is null)
            return Response<bool>.Fail("Size not found");
        hit.Value.Product.SizeStock.RemoveAll(s => s.Id == sizeStockId);
        await _store.UpsertProductAsync(hit.Value.Product);
        return Response<bool>.SuccessResponse(true);
    }
}
