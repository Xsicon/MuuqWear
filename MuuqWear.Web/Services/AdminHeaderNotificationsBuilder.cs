using MuuqWear.Application.Shared;
using MuuqWear.Model.NotificationModel;
using MuuqWear.Model.Products;

namespace MuuqWear.Web.Services;

/// <summary>
/// Builds admin header notifications. Low stock today; extend with more sources later.
/// </summary>
public static class AdminHeaderNotificationsBuilder
{
    public const int MaxLowStockItems = 15;

    public static List<ProductModel> OrderLowStockProducts(IEnumerable<ProductModel> products) =>
        products
            .Where(ProductStockHelper.IsLowStock)
            .OrderBy(p => ProductStockHelper.GetEffectiveStock(p))
            .ThenBy(p => p.Name)
            .ToList();

    public static List<NotificationModel> FromLowStockProducts(
        IEnumerable<ProductModel> orderedLowStockProducts,
        IReadOnlySet<Guid> readIds,
        IDictionary<Guid, DateTime>? firstSeenAt = null)
    {
        return orderedLowStockProducts
            .Take(MaxLowStockItems)
            .Select(p => ToLowStockNotification(p, readIds.Contains(p.Id), firstSeenAt))
            .ToList();
    }

    public static void UpdateFirstSeenTimes(
        IEnumerable<ProductModel> lowStockProducts,
        IDictionary<Guid, DateTime>? firstSeenAt)
    {
        if (firstSeenAt == null)
            return;

        var activeIds = new HashSet<Guid>();
        var now = DateTime.UtcNow;

        foreach (var product in lowStockProducts)
        {
            activeIds.Add(product.Id);
            if (!firstSeenAt.ContainsKey(product.Id))
                firstSeenAt[product.Id] = now;
        }

        foreach (var productId in firstSeenAt.Keys.Where(id => !activeIds.Contains(id)).ToList())
            firstSeenAt.Remove(productId);
    }

    private static NotificationModel ToLowStockNotification(
        ProductModel product,
        bool isRead,
        IDictionary<Guid, DateTime>? firstSeenAt)
    {
        var stock = ProductStockHelper.GetEffectiveStock(product);
        var name = string.IsNullOrWhiteSpace(product.Name) ? "Product" : product.Name.Trim();
        var createdAt = firstSeenAt != null
                        && firstSeenAt.TryGetValue(product.Id, out var seenAt)
            ? seenAt
            : DateTime.UtcNow;

        return new NotificationModel
        {
            Id = product.Id,
            Type = NotificationType.LowStock,
            Message = $"Low stock: {name}",
            ProductId = product.Id,
            Link = $"/admin/products?view=low-stock&productId={product.Id}",
            CreatedAt = createdAt,
            IsRead = isRead,
            TimeAgo = $"{stock} unit{(stock == 1 ? "" : "s")} left · below {ProductStockHelper.LowStockThreshold}"
        };
    }
}
