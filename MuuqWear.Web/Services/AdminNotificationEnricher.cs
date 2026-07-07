using System.Text.RegularExpressions;
using MuuqWear.Application.Shared;
using MuuqWear.Model.NotificationModel;
using MuuqWear.Model.Products;

namespace MuuqWear.Web.Services;

/// <summary>
/// Resolves navigation links for API-backed admin notifications (e.g. per-size low stock alerts).
/// </summary>
public static class AdminNotificationEnricher
{
    private static readonly Regex ProductAndSizePattern = new(
        @"Low stock alert:\s*(.+?)\s*\(Size\s+([^)]+)\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SizeOnlyPattern = new(
        @"Low stock alert:\s*Size\s+(\S+)\s*\(only\s+(\d+)\s+left\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool IsLowStockNotification(NotificationModel notification) =>
        notification.Type.Equals(NotificationType.LowStock, StringComparison.OrdinalIgnoreCase)
        || notification.Type.Equals(NotificationType.Stock, StringComparison.OrdinalIgnoreCase)
        || notification.Message.Contains("low stock", StringComparison.OrdinalIgnoreCase);

    public static void Enrich(NotificationModel notification, IReadOnlyList<ProductModel> products)
    {
        if (!IsLowStockNotification(notification))
            return;

        if (notification.Type.Equals(NotificationType.Stock, StringComparison.OrdinalIgnoreCase))
            notification.Type = NotificationType.LowStock;

        if (!string.IsNullOrWhiteSpace(notification.Link))
            return;

        if (notification.ProductId is Guid productId)
        {
            notification.Link = BuildProductLink(productId);
            return;
        }

        var productAndSize = ProductAndSizePattern.Match(notification.Message);
        if (productAndSize.Success)
        {
            var productName = productAndSize.Groups[1].Value.Trim();
            var size = productAndSize.Groups[2].Value.Trim();
            var product = FindByName(products, productName)
                          ?? FindBySize(products, size, null);

            if (product != null)
            {
                ApplyProduct(notification, product, size);
                return;
            }
        }

        var sizeOnly = SizeOnlyPattern.Match(notification.Message);
        if (sizeOnly.Success)
        {
            var size = sizeOnly.Groups[1].Value.Trim();
            if (int.TryParse(sizeOnly.Groups[2].Value, out var quantity))
            {
                var product = FindBySize(products, size, quantity)
                              ?? FindBySize(products, size, null);

                if (product != null)
                {
                    ApplyProduct(notification, product, size);
                    return;
                }
            }
        }

        notification.Link = "/admin/products?view=low-stock";
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

    private static void ApplyProduct(NotificationModel notification, ProductModel product, string? size)
    {
        notification.ProductId = product.Id;
        notification.SizeLabel = size;
        notification.Link = BuildProductLink(product.Id);
    }

    private static string BuildProductLink(Guid productId) =>
        $"/admin/products?view=low-stock&productId={productId}";

    private static ProductModel? FindByName(IReadOnlyList<ProductModel> products, string name) =>
        products.FirstOrDefault(p =>
            p.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true
            || p.Name?.Contains(name, StringComparison.OrdinalIgnoreCase) == true);

    private static ProductModel? FindBySize(
        IReadOnlyList<ProductModel> products,
        string size,
        int? quantity)
    {
        ProductModel? fallback = null;

        foreach (var product in products)
        {
            foreach (var sizeStock in product.SizeStock)
            {
                if (!sizeStock.Size.Equals(size, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (quantity.HasValue && sizeStock.Quantity == quantity.Value)
                    return product;

                fallback ??= product;
            }
        }

        return fallback;
    }
}
