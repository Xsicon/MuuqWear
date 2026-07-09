using MuuqWear.Application.Shared;
using MuuqWear.Model.Products;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminProductComponent
{
    private static BatchUpdateSizeStockRequest BuildBatchRequest(
        IEnumerable<SizeStockModel> sizes,
        Func<SizeStockModel, int> getQuantity)
    {
        var request = new BatchUpdateSizeStockRequest();

        foreach (var size in sizes)
        {
            var quantity = Math.Max(0, getQuantity(size));
            if (size.Id == Guid.Empty)
            {
                request.Upserts.Add(new BatchSizeStockUpsertItem
                {
                    Size = size.Size,
                    Quantity = quantity
                });
            }
            else
            {
                request.Items.Add(new BatchSizeStockUpdateItem
                {
                    SizeStockId = size.Id,
                    Quantity = quantity
                });
            }
        }

        return request;
    }

    private List<SizeStockModel> ResolveSizeStockForBulk(
        Guid productId,
        Response<List<SizeStockModel>> sizeStockResult)
    {
        if (sizeStockResult.Success && sizeStockResult.Data is { Count: > 0 })
            return sizeStockResult.Data;

        var product = Products.FirstOrDefault(p => p.Id == productId);
        if (product?.SizeStock is { Count: > 0 })
            return product.SizeStock;

        return new List<SizeStockModel>();
    }

    private static string GetProductLabel(ProductModel? product) =>
        string.IsNullOrWhiteSpace(product?.Name) ? "Product" : product!.Name.Trim();

    private void ApplySizeStockToProduct(
        Guid productId,
        List<SizeStockModel> sizeStock,
        ProductModel? fallbackProduct = null)
    {
        var product = Products.FirstOrDefault(p => p.Id == productId)
                      ?? (stockProduct?.Id == productId ? stockProduct : null)
                      ?? (fallbackProduct?.Id == productId ? fallbackProduct : null);

        if (product == null)
            return;

        product.SizeStock = sizeStock;
        ProductStockHelper.SyncStockFromSizes(product);

        var index = Products.FindIndex(p => p.Id == productId);
        if (index >= 0)
            Products[index] = product;
        else
            Products.Add(product);
    }

    private async Task<(bool Success, string? Error)> TryBatchUpdateSizeStockAsync(
        Guid productId,
        List<SizeStockModel> sizes,
        Func<SizeStockModel, int> getQuantity,
        ProductModel? fallbackProduct = null)
    {
        if (sizes.Count == 0)
            return (false, "No size stock rows to update.");

        var request = BuildBatchRequest(sizes, getQuantity);
        if (request.Items.Count == 0 && request.Upserts.Count == 0)
            return (false, "No size stock rows to update.");

        var result = await ProductService.UpdateSizeStockBatch(productId, request);
        if (!result.Success || result.Data?.SizeStock == null)
            return (false, result.Message ?? "Failed to update stock");

        ApplySizeStockToProduct(productId, result.Data.SizeStock, fallbackProduct);
        return (true, null);
    }

    private void ReportBulkPartialFailure(
        int updatedCount,
        int totalCount,
        string productLabel,
        string error,
        IReadOnlyList<Guid> successfulIds)
    {
        bulkError = updatedCount > 0
            ? $"Updated {updatedCount} of {totalCount} products. Failed on \"{productLabel}\": {error}"
            : $"Failed to update \"{productLabel}\": {error}";

        foreach (var id in successfulIds)
            selectedProductIds.Remove(id);

        if (successfulIds.Count > 0)
            ProductsTabCoordinator.RequestBadgeCountsRefresh();
    }
}
