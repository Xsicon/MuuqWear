using MuuqWear.Model.Products;
using MuuqWear.Model.Shared;

namespace MuuqWear.Application.Catalog;

public static class StorefrontCatalogMap
{
    public const string ImageHost = "https://firebasestorage.googleapis.com/v0/b/muuqwear-6e2e0.firebasestorage.app/o/";

    public static string ImageUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return path;
        return ImageHost + Uri.EscapeDataString(path.TrimStart('/')) + "?alt=media";
    }

    public static ProductModel Product(
        string id,
        string? name,
        long priceCents,
        string? status,
        IReadOnlyDictionary<string, long>? sizeStock,
        IReadOnlyList<string>? imagePaths,
        IReadOnlyList<string>? colors,
        IReadOnlyList<string>? tags,
        string? collectionId,
        string? description,
        string? sku)
    {
        Guid.TryParse(id, out var guid);
        Guid.TryParse(collectionId, out var categoryId);
        var published = string.Equals(status, "published", StringComparison.OrdinalIgnoreCase);
        var tagSet = new HashSet<string>((tags ?? Array.Empty<string>()).Select(t => t.ToLowerInvariant()));
        var images = (imagePaths ?? Array.Empty<string>()).Select((path, i) => new ProductImageModel
        {
            Id = Guid.NewGuid(),
            ProductId = guid,
            ImageUrl = ImageUrl(path),
            SortOrder = i
        }).ToList();
        var sizes = (sizeStock ?? new Dictionary<string, long>()).Select(pair => new SizeStockModel
        {
            Id = Guid.NewGuid(),
            Size = pair.Key,
            Quantity = (int)pair.Value
        }).ToList();
        return new ProductModel
        {
            Id = guid,
            Name = name,
            Price = priceCents / 100m,
            ImageUrl = images.FirstOrDefault()?.ImageUrl,
            Stock = sizes.Sum(s => s.Quantity),
            IsActive = published,
            Description = description,
            CategoryId = categoryId == Guid.Empty ? null : categoryId,
            CategoryName = tags?.FirstOrDefault(),
            Category = tags?.FirstOrDefault(),
            Sku = sku,
            SizeStock = sizes,
            Images = images,
            ColorOptions = (colors ?? Array.Empty<string>()).ToList(),
            IsNewArrival = tagSet.Contains("new") || tagSet.Contains("new-arrivals"),
            IsFeatured = tagSet.Contains("featured"),
            IsBestSeller = tagSet.Contains("bestseller") || tagSet.Contains("best-seller"),
        };
    }

    public static PaginatedResponse<ProductModel> Page(IReadOnlyList<ProductModel> items, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        var total = items.Count;
        var slice = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var pages = (int)Math.Ceiling(total / (double)pageSize);
        return new PaginatedResponse<ProductModel>
        {
            Data = slice,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = pages,
            HasMore = page < pages,
            HasPreviousPage = page > 1,
            HasNextPage = page < pages
        };
    }
}
