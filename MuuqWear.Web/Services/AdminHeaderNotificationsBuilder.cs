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

    public static List<NotificationModel> FromLowStockProducts(
        IEnumerable<ProductModel> products,
        IReadOnlySet<Guid> readIds)
    {
        return products
            .Where(ProductStockHelper.IsLowStock)
            .OrderBy(p => ProductStockHelper.GetEffectiveStock(p))
            .ThenBy(p => p.Name)
            .Take(MaxLowStockItems)
            .Select(p => ToLowStockNotification(p, readIds.Contains(p.Id)))
            .ToList();
    }

    private static NotificationModel ToLowStockNotification(ProductModel product, bool isRead)
    {
        var stock = ProductStockHelper.GetEffectiveStock(product);
        var name = string.IsNullOrWhiteSpace(product.Name) ? "Product" : product.Name.Trim();

        return new NotificationModel
        {
            Id = product.Id,
            Type = NotificationType.LowStock,
            Message = $"Low stock: {name}",
            Link = $"/admin/products?view=low-stock&productId={product.Id}",
            CreatedAt = DateTime.UtcNow,
            IsRead = isRead,
            TimeAgo = $"{stock} unit{(stock == 1 ? "" : "s")} left · below {ProductStockHelper.LowStockThreshold}"
        };
    }
}
