using MuuqWear.Application.Shared;
using MuuqWear.Model.NotificationModel;
using MuuqWear.Model.Products;

namespace MuuqWear.Web.Services;

/// <summary>
/// Normalizes API-backed admin notifications (e.g. per-size low stock alerts).
/// </summary>
public static class AdminNotificationEnricher
{
    public static bool IsLowStockNotification(NotificationModel notification) =>
        notification.Type.Equals(NotificationType.LowStock, StringComparison.OrdinalIgnoreCase)
        || notification.Type.Equals(NotificationType.Stock, StringComparison.OrdinalIgnoreCase)
        || notification.Message.Contains("low stock", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Normalizes low-stock notifications and validates product links against the loaded catalog.
    /// Products outside the loaded slice are left as-is (API is source of truth).
    /// Returns false when a stale low-stock alert should be dropped from the feed.
    /// </summary>
    public static bool Enrich(
        NotificationModel notification,
        IReadOnlyDictionary<Guid, ProductModel> productsById)
    {
        if (!IsLowStockNotification(notification))
            return true;

        if (notification.Type.Equals(NotificationType.Stock, StringComparison.OrdinalIgnoreCase))
            notification.Type = NotificationType.LowStock;

        if (!ValidateAgainstCatalog(notification, productsById))
            return false;

        if (!string.IsNullOrWhiteSpace(notification.Link))
            return true;

        notification.Link = notification.ProductId is Guid productId
            ? BuildProductLink(productId)
            : "/admin/products?view=low-stock";

        return true;
    }

    public static Guid? ResolveProductId(NotificationModel notification)
    {
        if (notification.ProductId is Guid productId)
            return productId;

        if (notification.Type == NotificationType.LowStock
            && notification.Link?.Contains("productId=", StringComparison.OrdinalIgnoreCase) == true)
        {
            var query = notification.Link.Split('?', 2).LastOrDefault() ?? string.Empty;
            foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.StartsWith("productId=", StringComparison.OrdinalIgnoreCase)
                    && Guid.TryParse(part["productId=".Length..], out var fromLink))
                    return fromLink;
            }
        }

        return null;
    }

    private static bool ValidateAgainstCatalog(
        NotificationModel notification,
        IReadOnlyDictionary<Guid, ProductModel> productsById)
    {
        if (notification.ProductId is not Guid productId)
            return true;

        if (!productsById.TryGetValue(productId, out var product))
            return true;

        if (IsStillLowStock(notification, product))
            return true;

        ClearProductContext(notification);
        return false;
    }

    private static bool IsStillLowStock(NotificationModel notification, ProductModel product)
    {
        if (notification.SizeStockId is Guid sizeStockId)
        {
            var size = product.SizeStock.FirstOrDefault(s => s.Id == sizeStockId);
            return size != null
                   && size.Quantity > 0
                   && size.Quantity < ProductStockHelper.LowStockThreshold;
        }

        if (!string.IsNullOrWhiteSpace(notification.SizeLabel)
            && product.SizeStock.Count > 0)
        {
            var size = product.SizeStock.FirstOrDefault(s =>
                s.Size.Equals(notification.SizeLabel, StringComparison.OrdinalIgnoreCase));

            return size != null
                   && size.Quantity > 0
                   && size.Quantity < ProductStockHelper.LowStockThreshold;
        }

        return ProductStockHelper.IsLowStock(product);
    }

    private static void ClearProductContext(NotificationModel notification)
    {
        notification.ProductId = null;
        notification.SizeStockId = null;
        notification.SizeLabel = null;
        notification.Link = "/admin/products?view=low-stock";
    }

    private static string BuildProductLink(Guid productId) =>
        $"/admin/products?view=low-stock&productId={productId}";
}
