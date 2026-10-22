using Google.Cloud.Firestore;
using MuuqWear.Application.Catalog;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Customer;
using MuuqWear.Model.Orders;
using MuuqWear.Model.Products;
using MuuqWear.Model.Shared;

namespace MuuqWear.Web.Services;

public sealed partial class AdminFirestore
{
    private static readonly TimeSpan SnapshotTtl = TimeSpan.FromSeconds(30);
    private readonly FirestoreDb _db;
    private readonly SnapCache _products = new();
    private readonly SnapCache _orders = new();
    private readonly SnapCache _customers = new();
    private readonly SnapCache _chats = new();
    private readonly SnapCache _affiliates = new();

    public AdminFirestore(IConfiguration config)
    {
        var projectId = config["Firebase:ProjectId"] ?? "muuqwear-6e2e0";
        _db = FirestoreDb.Create(projectId);
    }

    public async Task<List<ProductModel>> ListProductsAsync(CancellationToken cancellationToken = default)
    {
        var snap = await SnapshotAsync("products", _products, cancellationToken);
        return snap.Documents.Select(ToProduct).OrderBy(p => p.Name).ToList();
    }

    public async Task<ProductModel?> GetProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var snap = await _db.Collection("products").Document(id.ToString()).GetSnapshotAsync(cancellationToken);
        return snap.Exists ? ToProduct(snap) : null;
    }

    public async Task<ProductModel> UpsertProductAsync(ProductModel product, CancellationToken cancellationToken = default)
    {
        if (product.Id == Guid.Empty)
            product.Id = Guid.NewGuid();
        var tags = new List<string>();
        if (!string.IsNullOrWhiteSpace(product.CategoryName ?? product.Category))
            tags.Add((product.CategoryName ?? product.Category)!);
        if (product.IsNewArrival) tags.Add("new");
        if (product.IsFeatured) tags.Add("featured");
        if (product.IsBestSeller) tags.Add("bestseller");
        var sizeStock = product.SizeStock.ToDictionary(s => s.Size, s => (object)s.Quantity);
        var imagePaths = product.Images
            .Select(i => i.ImageUrl)
            .Concat(string.IsNullOrWhiteSpace(product.ImageUrl) ? [] : new[] { product.ImageUrl })
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u!)
            .Distinct()
            .ToList();
        var fields = new Dictionary<string, object>
        {
            ["name"] = product.Name ?? "",
            ["price"] = (long)Math.Round(product.Price * 100m),
            ["status"] = product.IsActive ? "published" : "draft",
            ["sizeStock"] = sizeStock,
            ["imagePaths"] = imagePaths.Select(ToStoragePath).ToList(),
            ["colors"] = product.ColorOptions,
            ["collectionTags"] = tags,
            ["createdAt"] = product.CreatedAt ?? DateTime.UtcNow
        };
        if (product.CategoryId is Guid categoryId)
            fields["collectionId"] = categoryId.ToString();
        if (!string.IsNullOrWhiteSpace(product.Description))
            fields["description"] = product.Description;
        if (!string.IsNullOrWhiteSpace(product.Sku))
            fields["sku"] = product.Sku;
        await _db.Collection("products").Document(product.Id.ToString()).SetAsync(fields, cancellationToken: cancellationToken);
        _products.Clear();
        return (await GetProductAsync(product.Id, cancellationToken))!;
    }

    public async Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _db.Collection("products").Document(id.ToString()).UpdateAsync(
            new Dictionary<string, object> { ["status"] = "archived" },
            cancellationToken: cancellationToken);
        return true;
    }

    public async Task<(ProductModel Product, SizeStockModel Size)?> FindSizeAsync(Guid sizeStockId, CancellationToken cancellationToken = default)
    {
        foreach (var product in await ListProductsAsync(cancellationToken))
        {
            var size = product.SizeStock.FirstOrDefault(s => s.Id == sizeStockId);
            if (size is not null)
                return (product, size);
        }
        return null;
    }

    public async Task<List<SizeStockModel>> GetSizeStockAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await GetProductAsync(productId, cancellationToken);
        return product?.SizeStock ?? new List<SizeStockModel>();
    }

    public async Task UpdateSizeStockAsync(Guid productId, IReadOnlyDictionary<string, int> sizeStock, CancellationToken cancellationToken = default)
    {
        var fields = sizeStock.ToDictionary(p => p.Key, p => (object)p.Value);
        await _db.Collection("products").Document(productId.ToString()).UpdateAsync(
            new Dictionary<string, object> { ["sizeStock"] = fields },
            cancellationToken: cancellationToken);
    }

    public async Task<PaginatedResponse<OrderModel>> ListOrdersAsync(
        string? status, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var snap = await SnapshotAsync("orders", _orders, cancellationToken);
        IEnumerable<OrderModel> query = snap.Documents.Select(ToOrder)
            .Where(o => AdminDirectoryFilter.IsShopperEmail(o.Email));
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => string.Equals(o.Status, status, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o =>
                (o.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                || (o.OrderNumber ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                || (o.ItemsSummary ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));
        var all = query.OrderByDescending(o => o.CreatedAt).ToList();
        return Page(all, page, pageSize);
    }

    public async Task<List<OrderModel>> OrdersForCustomerAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var snap = await SnapshotAsync("orders", _orders, cancellationToken);
        return snap.Documents
            .Where(d => string.Equals(Str(d, "customerId"), customerId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(Str(d, "email"), customerId, StringComparison.OrdinalIgnoreCase))
            .Select(ToOrder)
            .OrderByDescending(o => o.CreatedAt)
            .ToList();
    }

    public async Task<OrderModel?> GetOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var doc = await FindOrderDocAsync(id, cancellationToken);
        return doc is null ? null : ToOrder(doc);
    }

    public async Task<OrderModel?> GetOrderByNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            return null;
        var needle = orderNumber.Trim().TrimStart('#');
        var snap = await SnapshotAsync("orders", _orders, cancellationToken);
        return snap.Documents.Select(ToOrder).FirstOrDefault(o =>
            string.Equals(o.OrderNumber, needle, StringComparison.OrdinalIgnoreCase)
            || string.Equals(o.FirestoreId, needle, StringComparison.OrdinalIgnoreCase)
            || (o.FirestoreId ?? "").StartsWith(needle, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<OrderModel?> UpdateOrderStatusAsync(Guid id, string status, CancellationToken cancellationToken = default)
    {
        var doc = await FindOrderDocAsync(id, cancellationToken);
        if (doc is null)
            return null;
        await doc.Reference.UpdateAsync("status", status, cancellationToken: cancellationToken);
        _orders.Clear();
        return await GetOrderAsync(id, cancellationToken);
    }

    public async Task<PaginatedResponse<CustomerModel>> ListCustomersAsync(
        string? search, int page, int pageSize, string? status, CancellationToken cancellationToken = default)
    {
        var customers = await SnapshotAsync("customers", _customers, cancellationToken);
        var orders = await SnapshotAsync("orders", _orders, cancellationToken);
        var spent = orders.Documents
            .GroupBy(d => Str(d, "customerId"))
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var list = new List<CustomerModel>();
        foreach (var doc in customers.Documents)
        {
            var model = ToCustomer(doc, spent.GetValueOrDefault(doc.Id) ?? []);
            if (AdminDirectoryFilter.IsShopperEmail(model.Email))
                list.Add(model);
        }

        IEnumerable<CustomerModel> query = list;
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => string.Equals(c.AccountStatus, status, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c =>
                (c.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                || (c.FullName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));
        var all = query.OrderByDescending(c => c.CreatedAt).ToList();
        return Page(all, page, pageSize);
    }

    public async Task<CustomerModel?> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var doc = await FindCustomerDocAsync(customerId, cancellationToken);
        if (doc is null)
            return null;
        var orders = await _db.Collection("orders").GetSnapshotAsync(cancellationToken);
        var theirs = orders.Documents.Where(d => string.Equals(Str(d, "customerId"), doc.Id, StringComparison.OrdinalIgnoreCase)).ToList();
        return ToCustomer(doc, theirs);
    }

    public async Task<List<CustomerNoteModel>> GetNotesAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var doc = await FindCustomerDocAsync(customerId, cancellationToken);
        if (doc is null)
            return [];
        var snap = await doc.Reference.Collection("notes").GetSnapshotAsync(cancellationToken);
        return snap.Documents.Select(n => ToNote(n, customerId)).OrderByDescending(n => n.CreatedAt).ToList();
    }

    public async Task<CustomerNoteModel> AddNoteAsync(Guid customerId, string body, string authorName, string? authorRole, CancellationToken cancellationToken = default)
    {
        var doc = await FindCustomerDocAsync(customerId, cancellationToken)
            ?? throw new InvalidOperationException("Customer not found");
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await doc.Reference.Collection("notes").Document(id.ToString()).SetAsync(new Dictionary<string, object>
        {
            ["body"] = body,
            ["authorName"] = authorName,
            ["authorRole"] = authorRole ?? "",
            ["createdAt"] = now
        }, cancellationToken: cancellationToken);
        return new CustomerNoteModel
        {
            Id = id,
            CustomerId = customerId,
            Body = body,
            AuthorName = authorName,
            AuthorRole = authorRole,
            CreatedAt = now
        };
    }

    public async Task<CustomerModel?> SuspendAsync(Guid customerId, int days, string? reason, CancellationToken cancellationToken = default)
    {
        var doc = await FindCustomerDocAsync(customerId, cancellationToken);
        if (doc is null)
            return null;
        await doc.Reference.UpdateAsync(new Dictionary<string, object>
        {
            ["accountStatus"] = "suspended",
            ["suspendedUntil"] = DateTime.UtcNow.AddDays(Math.Max(1, days)),
            ["suspensionReason"] = reason ?? "",
            ["suspendedAt"] = DateTime.UtcNow
        }, cancellationToken: cancellationToken);
        return await GetCustomerAsync(customerId, cancellationToken);
    }

    public async Task<CustomerModel?> ReactivateAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var doc = await FindCustomerDocAsync(customerId, cancellationToken);
        if (doc is null)
            return null;
        await doc.Reference.UpdateAsync(new Dictionary<string, object>
        {
            ["accountStatus"] = "active",
            ["suspensionReason"] = "",
        }, cancellationToken: cancellationToken);
        return await GetCustomerAsync(customerId, cancellationToken);
    }

    private async Task<DocumentSnapshot?> FindCustomerDocAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var byId = await _db.Collection("customers").Document(customerId.ToString()).GetSnapshotAsync(cancellationToken);
        if (byId.Exists)
            return byId;
        var all = await _db.Collection("customers").GetSnapshotAsync(cancellationToken);
        return all.Documents.FirstOrDefault(d =>
            string.Equals(Str(d, "profileId"), customerId.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    private static ProductModel ToProduct(DocumentSnapshot doc)
    {
        var sizeStock = new Dictionary<string, long>();
        if (doc.TryGetValue("sizeStock", out Dictionary<string, object>? raw) && raw is not null)
        {
            foreach (var pair in raw)
                sizeStock[pair.Key] = Convert.ToInt64(pair.Value);
        }
        var product = StorefrontCatalogMap.Product(
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
        if (doc.ContainsField("createdAt"))
            product.CreatedAt = ToDate(doc, "createdAt");
        foreach (var size in product.SizeStock)
            size.Id = StaffSessionJwt.StableUserGuid(product.Id + ":" + size.Size);
        return product;
    }

    private async Task<DocumentSnapshot?> FindOrderDocAsync(Guid id, CancellationToken cancellationToken)
    {
        var direct = await _db.Collection("orders").Document(id.ToString()).GetSnapshotAsync(cancellationToken);
        if (direct.Exists)
            return direct;

        var snap = await _db.Collection("orders").GetSnapshotAsync(cancellationToken);
        return snap.Documents.FirstOrDefault(d => ToOrder(d).Id == id);
    }

    private static OrderModel ToOrder(DocumentSnapshot doc)
    {
        Guid.TryParse(doc.Id, out var parsed);
        var id = parsed != Guid.Empty ? parsed : StaffSessionJwt.StableUserGuid(doc.Id);
        var items = ParseItems(doc);
        var orderNumber = Str(doc, "orderNumber");
        if (string.IsNullOrWhiteSpace(orderNumber))
            orderNumber = doc.Id[..Math.Min(8, doc.Id.Length)].ToUpperInvariant();

        return new OrderModel
        {
            Id = id,
            FirestoreId = doc.Id,
            OrderNumber = orderNumber,
            Email = Str(doc, "email"),
            Status = Str(doc, "status"),
            Subtotal = Cents(doc, "subtotal"),
            Shipping = Cents(doc, "shipping"),
            Tax = Cents(doc, "tax"),
            Total = Cents(doc, "total"),
            CreatedAt = ToDate(doc, "createdAt"),
            Items = items,
            ItemsSummary = string.Join(", ", items.Select(i => i.ProductName).Where(n => !string.IsNullOrWhiteSpace(n)))
        };
    }

    private static CustomerModel ToCustomer(DocumentSnapshot doc, IReadOnlyList<DocumentSnapshot> orders)
    {
        Guid.TryParse(Str(doc, "profileId"), out var profileId);
        if (profileId == Guid.Empty)
            Guid.TryParse(doc.Id, out profileId);
        var paid = orders.Where(o =>
        {
            var status = Str(o, "status");
            var payment = Str(o, "paymentStatus");
            return !string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(payment, "unpaid", StringComparison.OrdinalIgnoreCase);
        }).ToList();
        return new CustomerModel
        {
            Id = profileId == Guid.Empty ? StaffSessionJwtGuid(doc.Id) : profileId,
            FullName = Str(doc, "name"),
            Email = Str(doc, "email"),
            CreatedAt = ToDate(doc, "createdAt"),
            OrderCount = paid.Count,
            TotalSpent = paid.Sum(o => Cents(o, "total")),
            LastOrderAt = paid.Select(o => ToDate(o, "createdAt")).Where(d => d.HasValue).Max(),
            AccountStatus = string.IsNullOrWhiteSpace(Str(doc, "accountStatus")) ? "active" : Str(doc, "accountStatus"),
            SuspendedUntil = ToDate(doc, "suspendedUntil"),
            SuspensionReason = Str(doc, "suspensionReason"),
            SuspendedAt = ToDate(doc, "suspendedAt")
        };
    }

    private static Guid StaffSessionJwtGuid(string id) => StaffSessionJwt.StableUserGuid(id);

    private static CustomerNoteModel ToNote(DocumentSnapshot doc, Guid customerId)
    {
        Guid.TryParse(doc.Id, out var id);
        return new CustomerNoteModel
        {
            Id = id,
            CustomerId = customerId,
            AuthorName = Str(doc, "authorName"),
            AuthorRole = Str(doc, "authorRole"),
            Body = Str(doc, "body"),
            CreatedAt = ToDate(doc, "createdAt") ?? DateTime.UtcNow
        };
    }

    private static PaginatedResponse<T> Page<T>(IReadOnlyList<T> items, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        var total = items.Count;
        var pages = (int)Math.Ceiling(total / (double)pageSize);
        return new PaginatedResponse<T>
        {
            Data = items.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = pages,
            HasMore = page < pages,
            HasPreviousPage = page > 1,
            HasNextPage = page < pages
        };
    }

    private static List<string> Strings(DocumentSnapshot doc, string field)
    {
        if (!doc.ContainsField(field))
            return [];
        try { return doc.GetValue<List<string>>(field) ?? []; }
        catch { return []; }
    }

    private static string Str(DocumentSnapshot doc, string field) =>
        doc.ContainsField(field) ? Convert.ToString(doc.GetValue<object>(field)) ?? "" : "";

    private static List<OrderItemModel> ParseItems(DocumentSnapshot doc)
    {
        var items = new List<OrderItemModel>();
        if (!doc.ContainsField("items"))
            return items;
        IEnumerable<object> rows;
        try { rows = doc.GetValue<List<object>>("items") ?? []; }
        catch { return items; }
        foreach (var rowObj in rows)
        {
            Dictionary<string, object> row;
            if (rowObj is Dictionary<string, object> dict)
                row = dict;
            else if (rowObj is IDictionary<string, object> map)
                row = map.ToDictionary(p => p.Key, p => p.Value);
            else
                continue;
            Guid.TryParse(StrObj(row, "id"), out var itemId);
            Guid.TryParse(StrObj(row, "productId"), out var productId);
            var qty = ToInt(row, "quantity");
            var price = Cents(row, "price");
            items.Add(new OrderItemModel
            {
                Id = itemId,
                ProductId = productId,
                ProductName = StrObj(row, "name"),
                Size = StrObj(row, "size"),
                Quantity = qty,
                Price = price,
                ItemTotal = row.ContainsKey("total") ? Cents(row, "total") : price * qty
            });
        }
        return items;
    }

    private static string StrObj(Dictionary<string, object> row, string field) =>
        row.TryGetValue(field, out var value) ? Convert.ToString(value) ?? "" : "";

    private static int ToInt(Dictionary<string, object> row, string field) =>
        row.TryGetValue(field, out var value) ? Convert.ToInt32(value) : 0;

    private static decimal Cents(DocumentSnapshot doc, string field) =>
        doc.ContainsField(field) ? Convert.ToInt64(doc.GetValue<object>(field)) / 100m : 0;

    private static decimal Cents(Dictionary<string, object> row, string field) =>
        row.TryGetValue(field, out var value) ? Convert.ToInt64(value) / 100m : 0;

    private static DateTime? ToDate(DocumentSnapshot doc, string field)
    {
        if (!doc.ContainsField(field))
            return null;
        try
        {
            var ts = doc.GetValue<Timestamp>(field);
            return ts.ToDateTime();
        }
        catch
        {
            try { return doc.GetValue<DateTime>(field); }
            catch { return null; }
        }
    }

    private static string ToStoragePath(string url)
    {
        const string host = StorefrontCatalogMap.ImageHost;
        if (url.StartsWith(host, StringComparison.OrdinalIgnoreCase))
        {
            var rest = url[host.Length..];
            var q = rest.IndexOf('?', StringComparison.Ordinal);
            if (q >= 0) rest = rest[..q];
            return Uri.UnescapeDataString(rest);
        }
        return url;
    }

    private async Task<QuerySnapshot> SnapshotAsync(string collection, SnapCache cache, CancellationToken cancellationToken)
    {
        if (cache.IsFresh(SnapshotTtl))
            return cache.Snap!;
        var snap = await _db.Collection(collection).GetSnapshotAsync(cancellationToken);
        cache.Set(snap);
        return snap;
    }

    private sealed class SnapCache
    {
        private readonly object _gate = new();
        public QuerySnapshot? Snap { get; private set; }
        private DateTime _at;

        public bool IsFresh(TimeSpan ttl)
        {
            lock (_gate)
                return Snap is not null && DateTime.UtcNow - _at < ttl;
        }

        public void Set(QuerySnapshot snap)
        {
            lock (_gate)
            {
                Snap = snap;
                _at = DateTime.UtcNow;
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                Snap = null;
                _at = default;
            }
        }
    }
}
