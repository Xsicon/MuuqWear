using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Products;
using MuuqWear.Web.Services;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminProductComponent
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private bool exportMenuOpen;
    private bool isExporting;
    private string exportMessage = string.Empty;
    private const int ExportPageSize = 100;

    private void ToggleExportMenu() => exportMenuOpen = !exportMenuOpen;

    private void CloseExportMenu() => exportMenuOpen = false;

    private string CurrentFilterLabel => activeFilter switch
    {
        "All" => "All",
        "LowStock" => "Low Stock",
        "OutOfStock" => "Out of Stock",
        _ => Categories.FirstOrDefault(c => c.Id.ToString() == activeFilter)?.Name ?? activeFilter
    };

    private string CurrentViewLabel => activeView switch
    {
        "stock" => "Stock Levels",
        "low-stock" => "Low Stock Alerts",
        "restock" => "Restock Requests",
        _ => "Product Catalog"
    };

    private async Task ExportAsync(string format, bool selectedOnly = false)
    {
        if (isExporting)
            return;

        exportMenuOpen = false;
        isExporting = true;
        exportMessage = string.Empty;
        StateHasChanged();

        try
        {
            List<ProductModel> exportProducts;

            if (selectedOnly)
            {
                exportProducts = Products
                    .Where(p => selectedProductIds.Contains(p.Id))
                    .ToList();

                if (exportProducts.Count == 0)
                {
                    exportMessage = "No selected products to export.";
                    return;
                }
            }
            else
            {
                exportProducts = await FetchAllProductsForExportAsync();
                if (exportProducts.Count == 0)
                {
                    exportMessage = "No products to export for the current filter.";
                    return;
                }
            }

            byte[] bytes;
            string fileName;
            string mimeType;
            var filterLabel = selectedOnly ? "selected" : CurrentFilterLabel;

            if (format == "csv")
            {
                bytes = AdminProductsExportBuilder.BuildCsv(
                    exportProducts, filterLabel, CurrentViewLabel);
                fileName = AdminProductsExportBuilder.BuildFileName("csv", filterLabel, selectedOnly);
                mimeType = "text/csv;charset=utf-8";
            }
            else
            {
                bytes = AdminProductsExportBuilder.BuildExcel(
                    exportProducts, filterLabel, CurrentViewLabel);
                fileName = AdminProductsExportBuilder.BuildFileName("xlsx", filterLabel, selectedOnly);
                mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            }

            await JS.InvokeVoidAsync(
                "adminExport.downloadBytes",
                fileName,
                Convert.ToBase64String(bytes),
                mimeType);

            exportMessage = $"Exported {exportProducts.Count} product{(exportProducts.Count == 1 ? "" : "s")}.";
        }
        catch (Exception ex)
        {
            exportMessage = $"Export failed: {AdminUiErrorHelper.FromException(ex)}";
        }
        finally
        {
            isExporting = false;
            StateHasChanged();
        }
    }

    private async Task<List<ProductModel>> FetchAllProductsForExportAsync()
    {
        Guid? categoryId = null;
        if (activeFilter != "All" &&
            activeFilter != "LowStock" &&
            activeFilter != "OutOfStock" &&
            Guid.TryParse(activeFilter, out var parsedId))
        {
            categoryId = parsedId;
        }

        var isStockFilter = activeFilter == "LowStock" || activeFilter == "OutOfStock";
        var all = new List<ProductModel>();
        var page = 1;
        var pageSize = isStockFilter ? 1000 : ExportPageSize;

        while (true)
        {
            var result = await ProductService.GetAll(new ProductFilterModel
            {
                Page = page,
                PageSize = pageSize,
                Search = isStockFilter || string.IsNullOrEmpty(searchQuery) ? null : searchQuery,
                CategoryId = isStockFilter ? null : categoryId
            });

            if (!result.Success || result.Data?.Data is null)
                throw new InvalidOperationException(
                    AdminUiErrorHelper.FromApi(result.Message, "Failed to fetch products for export."));

            all.AddRange(result.Data.Data);

            if (isStockFilter || all.Count >= result.Data.TotalCount || result.Data.Data.Count == 0)
                break;

            page++;
        }

        foreach (var product in all)
            ProductStockHelper.SyncStockFromSizes(product);

        IEnumerable<ProductModel> filtered = activeFilter switch
        {
            "LowStock" => all.Where(ProductStockHelper.IsLowStock),
            "OutOfStock" => all.Where(ProductStockHelper.IsOutOfStock),
            _ => all
        };

        if (isStockFilter && !string.IsNullOrEmpty(searchQuery))
            filtered = ApplySearch(filtered);
        else if (!isStockFilter && string.IsNullOrEmpty(searchQuery) == false)
            filtered = ApplySearch(filtered);

        return filtered.ToList();
    }
}
