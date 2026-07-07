using MuuqWear.Model.Products;

namespace MuuqWear.Application.Shared;

/// <summary>
/// Stock status helpers. Per-size inventory is the source of truth when SizeStock exists;
/// the product Stock column may be stale if only size rows were updated.
/// </summary>
public static class ProductStockHelper
{
    public const int LowStockThreshold = 5;

    public static int GetEffectiveStock(ProductModel product) =>
        product.SizeStock.Count > 0
            ? product.SizeStock.Sum(s => s.Quantity)
            : product.Stock;

    public static bool IsOutOfStock(ProductModel product) =>
        GetEffectiveStock(product) == 0;

    public static bool IsLowStock(ProductModel product)
    {
        if (product.SizeStock.Count > 0
            && product.SizeStock.Any(s => s.Quantity > 0 && s.Quantity < LowStockThreshold))
            return true;

        var stock = GetEffectiveStock(product);
        return stock > 0 && stock < LowStockThreshold;
    }

    public static void SyncStockFromSizes(ProductModel product)
    {
        if (product.SizeStock.Count > 0)
            product.Stock = product.SizeStock.Sum(s => s.Quantity);
    }
}
