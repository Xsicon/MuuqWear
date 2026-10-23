using Google.Cloud.Firestore;
using MuuqWear.Application.Catalog;
using MuuqWear.Application.Shared;
using MuuqWear.Model.ContentItem;
using MuuqWear.Model.Products;
using MuuqWear.Model.Shared;

namespace MuuqWear.Web.Services;

public sealed class StorefrontFirestore
{
    private readonly FirestoreDb _db;
    private readonly AdminFirestore _store;

    public StorefrontFirestore(IConfiguration config, AdminFirestore store)
    {
        var projectId = config["Firebase:ProjectId"] ?? "muuqwear-6e2e0";
        _db = FirestoreDb.Create(projectId);
        _store = store;
    }

    public async Task<object> HandleAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        var path = (request.Path.Value ?? "").Trim('/');
        if (path.Equals("api/Product/home", StringComparison.OrdinalIgnoreCase))
            return Response<HomeProductsModel>.SuccessResponse(await HomeAsync(cancellationToken));
        if (path.Equals("api/Product/all", StringComparison.OrdinalIgnoreCase))
            return await AllAsync(request, cancellationToken);
        if (path.Equals("api/Category/all", StringComparison.OrdinalIgnoreCase))
            return Response<List<CategoryModel>>.SuccessResponse(await CategoriesAsync(cancellationToken));
        if (path.Equals("api/Journal", StringComparison.OrdinalIgnoreCase))
            return await JournalAsync(request, cancellationToken);

        var journalOne = System.Text.RegularExpressions.Regex.Match(path, @"^api/Journal/([^/]+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (journalOne.Success)
        {
            Guid.TryParse(journalOne.Groups[1].Value, out var id);
            var item = (await _store.ListContentAsync(ContentCategory.JournalArticles, cancellationToken))
                .FirstOrDefault(i => i.Id == id);
            return item is null
                ? Response<ContentItemModel>.Fail("Article not found")
                : Response<ContentItemModel>.SuccessResponse(item);
        }

        var related = System.Text.RegularExpressions.Regex.Match(path, @"^api/Product/([^/]+)/related$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (related.Success)
            return Response<List<ProductModel>>.SuccessResponse(await RelatedAsync(related.Groups[1].Value, cancellationToken));

        var one = System.Text.RegularExpressions.Regex.Match(path, @"^api/Product/([^/]+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (one.Success)
        {
            var product = await ByIdAsync(one.Groups[1].Value, cancellationToken);
            return product is null
                ? Response<ProductModel>.Fail("Product not found")
                : Response<ProductModel>.SuccessResponse(product);
        }

        return Response<object>.Fail("Not found");
    }

    private async Task<object> JournalAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        var articles = (await _store.ListContentAsync(ContentCategory.JournalArticles, cancellationToken))
            .Where(i => i.IsPublished)
            .ToList();
        if (!int.TryParse(request.Query["page"], out var page))
            return Response<List<ContentItemModel>>.SuccessResponse(articles);

        if (!int.TryParse(request.Query["pageSize"], out var pageSize) || pageSize < 1)
            pageSize = 6;
        if (page < 1)
            page = 1;
        var category = request.Query["category"].ToString();
        if (!string.IsNullOrWhiteSpace(category))
            articles = articles.Where(i => string.Equals(i.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
        var total = articles.Count;
        var pages = (int)Math.Ceiling(total / (double)pageSize);
        return Response<PaginatedResponse<ContentItemModel>>.SuccessResponse(new PaginatedResponse<ContentItemModel>
        {
            Data = articles.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = pages
        });
    }

    private async Task<List<ProductModel>> PublishedAsync(CancellationToken cancellationToken)
    {
        var snap = await _db.Collection("products").GetSnapshotAsync(cancellationToken);
        return snap.Documents
            .Select(ToProduct)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToList();
    }

    private async Task<HomeProductsModel> HomeAsync(CancellationToken cancellationToken)
    {
        var all = await PublishedAsync(cancellationToken);
        return new HomeProductsModel
        {
            NewArrivals = TakeOrFill(all, p => p.IsNewArrival, 6),
            Featured = TakeOrFill(all, p => p.IsFeatured, 6),
            BestSellers = TakeOrFill(all, p => p.IsBestSeller, 6)
        };
    }

    private async Task<object> AllAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        var all = await PublishedAsync(cancellationToken);
        var search = request.Query["search"].ToString();
        var sizes = request.Query["sizes"].ToString();
        var sort = request.Query["sortBy"].ToString();
        if (!int.TryParse(request.Query["page"], out var page)) page = 1;
        if (!int.TryParse(request.Query["pageSize"], out var pageSize)) pageSize = 10;
        decimal.TryParse(request.Query["minPrice"], out var minPrice);
        decimal.TryParse(request.Query["maxPrice"], out var maxPrice);

        IEnumerable<ProductModel> query = all;
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => (p.Name ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(sizes))
        {
            var wanted = sizes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            query = query.Where(p => p.SizeStock.Any(s => wanted.Contains(s.Size, StringComparer.OrdinalIgnoreCase)));
        }
        if (minPrice > 0)
            query = query.Where(p => p.Price >= minPrice);
        if (maxPrice > 0)
            query = query.Where(p => p.Price <= maxPrice);
        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderBy(p => p.Name)
        };
        return Response<PaginatedResponse<ProductModel>>.SuccessResponse(
            StorefrontCatalogMap.Page(query.ToList(), page, pageSize));
    }

    private async Task<ProductModel?> ByIdAsync(string id, CancellationToken cancellationToken)
    {
        var snap = await _db.Collection("products").Document(id).GetSnapshotAsync(cancellationToken);
        if (!snap.Exists)
            return null;
        return ToProduct(snap);
    }

    private async Task<List<ProductModel>> RelatedAsync(string id, CancellationToken cancellationToken)
    {
        var current = await ByIdAsync(id, cancellationToken);
        var all = await PublishedAsync(cancellationToken);
        return all.Where(p => p.Id != current?.Id && p.CategoryId == current?.CategoryId).Take(4).ToList();
    }

    private async Task<List<CategoryModel>> CategoriesAsync(CancellationToken cancellationToken)
    {
        var snap = await _db.Collection("collections").GetSnapshotAsync(cancellationToken);
        return snap.Documents.Select(doc =>
        {
            Guid.TryParse(doc.Id, out var id);
            return new CategoryModel { Id = id, Name = doc.GetValue<string>("name") };
        }).OrderBy(c => c.Name).ToList();
    }

    private static List<ProductModel> TakeOrFill(List<ProductModel> all, Func<ProductModel, bool> match, int take)
    {
        var hit = all.Where(match).Take(take).ToList();
        if (hit.Count >= take)
            return hit;
        hit.AddRange(all.Where(p => hit.All(x => x.Id != p.Id)).Take(take - hit.Count));
        return hit;
    }

    private static ProductModel ToProduct(DocumentSnapshot doc)
    {
        var sizeStock = new Dictionary<string, long>();
        if (doc.TryGetValue("sizeStock", out Dictionary<string, object>? raw) && raw is not null)
        {
            foreach (var pair in raw)
                sizeStock[pair.Key] = Convert.ToInt64(pair.Value);
        }

        return StorefrontCatalogMap.Product(
            doc.Id,
            doc.ContainsField("name") ? doc.GetValue<string>("name") : doc.Id,
            doc.ContainsField("price") ? Convert.ToInt64(doc.GetValue<object>("price")) : 0,
            doc.ContainsField("status") ? doc.GetValue<string>("status") : "",
            sizeStock,
            Strings(doc, "imagePaths"),
            Strings(doc, "colors"),
            Strings(doc, "collectionTags"),
            doc.ContainsField("collectionId") ? doc.GetValue<string>("collectionId") : null,
            doc.ContainsField("description") ? doc.GetValue<string>("description") : null,
            doc.ContainsField("sku") ? doc.GetValue<string>("sku") : null);
    }

    private static List<string> Strings(DocumentSnapshot doc, string field)
    {
        if (!doc.ContainsField(field))
            return new List<string>();
        try
        {
            return doc.GetValue<List<string>>(field) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
