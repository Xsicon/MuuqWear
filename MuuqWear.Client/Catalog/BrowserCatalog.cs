using Microsoft.JSInterop;
using MuuqWear.Application.Catalog;
using MuuqWear.Model.ContentItem;
using MuuqWear.Model.Products;
using MuuqWear.Model.Shared;

namespace MuuqWear.Client.Catalog;

public sealed class BrowserCatalog
{
    private readonly IJSRuntime _js;
    private List<ProductModel>? _products;
    private List<CategoryModel>? _categories;
    private List<ContentItemModel>? _journal;

    public BrowserCatalog(IJSRuntime js) => _js = js;

    public async Task<List<ProductModel>> ProductsAsync()
    {
        if (_products is not null)
            return _products;
        var rows = await ReadAsync("muuqFirebase.listProducts");
        _products = rows
            .Select(ToProduct)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToList();
        return _products;
    }

    public async Task<ProductModel?> ProductAsync(string id)
    {
        var all = await ProductsAsync();
        if (Guid.TryParse(id, out var guid))
        {
            var hit = all.FirstOrDefault(p => p.Id == guid);
            if (hit is not null)
                return hit;
        }

        try
        {
            var row = await _js.InvokeAsync<CatalogDoc?>("muuqFirebase.getProduct", id);
            return row is null ? null : ToProduct(row);
        }
        catch (JSException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public async Task<List<CategoryModel>> CategoriesAsync()
    {
        if (_categories is not null)
            return _categories;
        var rows = await ReadAsync("muuqFirebase.listCollections");
        _categories = rows
            .Select(row =>
            {
                Guid.TryParse(row.Id, out var id);
                return new CategoryModel { Id = id, Name = row.Name };
            })
            .OrderBy(c => c.Name)
            .ToList();
        if (_categories.Count == 0)
        {
            _categories = (await ProductsAsync())
                .Where(p => p.CategoryId is Guid)
                .GroupBy(p => p.CategoryId!.Value)
                .Select(g => new CategoryModel
                {
                    Id = g.Key,
                    Name = g.Select(p => p.CategoryName ?? p.Category).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "Shop"
                })
                .OrderBy(c => c.Name)
                .ToList();
        }
        return _categories;
    }

    public async Task<List<ContentItemModel>> JournalAsync()
    {
        if (_journal is not null)
            return _journal;
        var rows = await ReadAsync("muuqFirebase.listJournal");
        var type = ContentCategory.JournalArticles.ToString();
        _journal = rows
            .Select(ToJournal)
            .Where(i => i.IsPublished)
            .Where(i => string.IsNullOrWhiteSpace(i.Category)
                        || string.Equals(i.Category, type, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(i => i.CreatedAt)
            .ToList();
        return _journal;
    }

    private async Task<List<CatalogDoc>> ReadAsync(string name)
    {
        try
        {
            var rows = await _js.InvokeAsync<List<CatalogDoc>>(name);
            return rows ?? [];
        }
        catch (JSException)
        {
            return [];
        }
        catch (InvalidOperationException)
        {
            return [];
        }
    }

    private static ProductModel ToProduct(CatalogDoc row) =>
        StorefrontCatalogMap.Product(
            row.Id,
            row.Name,
            row.Price,
            row.Status,
            row.SizeStock,
            row.ImagePaths,
            row.Colors,
            row.CollectionTags,
            row.CollectionId,
            row.Description,
            row.Sku);

    private static ContentItemModel ToJournal(CatalogDoc row)
    {
        Guid.TryParse(row.Id, out var id);
        return new ContentItemModel
        {
            Id = id,
            Title = row.Title ?? row.Name ?? "",
            Content = row.Body,
            Status = string.IsNullOrWhiteSpace(row.Status) ? "draft" : row.Status,
            Category = row.Category,
            ImageUrl = row.ImageUrl,
            Author = row.Author,
            Excerpt = row.Excerpt,
            Slug = row.Slug,
            CreatedAt = row.CreatedAt
        };
    }
}
