using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MuuqWear.Model.OrderReturn;
using MuuqWear.Model.Orders;
using MuuqWear.Model.Refund;
using MuuqWear.Web.Services;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminOrdersComponent
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private bool exportMenuOpen;
    private bool isExporting;
    private string exportMessage = string.Empty;
    private const int ExportPageSize = 100;

    private string? CurrentStatusFilter => activeMainTab switch
    {
        "returns" => activeReturnStatus,
        "refunds" => activeRefundStatus,
        _ => activeStatus
    };

    private void ToggleExportMenu() => exportMenuOpen = !exportMenuOpen;

    private void CloseExportMenu() => exportMenuOpen = false;

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
            var tab = activeMainTab;
            var filter = CurrentStatusFilter;

            List<OrderModel> exportOrders = new();
            List<OrderReturnModel> exportReturns = new();
            List<RefundModel> exportRefunds = new();

            switch (tab)
            {
                case "returns":
                    exportReturns = await FetchAllReturnsForExportAsync();
                    if (exportReturns.Count == 0)
                    {
                        exportMessage = "No returns to export for the current filter.";
                        return;
                    }
                    break;
                case "refunds":
                    exportRefunds = await FetchAllRefundsForExportAsync();
                    if (exportRefunds.Count == 0)
                    {
                        exportMessage = "No refunds to export for the current filter.";
                        return;
                    }
                    break;
                default:
                    if (selectedOnly)
                    {
                        exportOrders = orders
                            .Where(o => selectedOrderIds.Contains(o.Id))
                            .ToList();

                        if (exportOrders.Count == 0)
                        {
                            exportMessage = "No selected orders to export.";
                            return;
                        }
                    }
                    else
                    {
                        exportOrders = await FetchAllOrdersForExportAsync();
                        if (exportOrders.Count == 0)
                        {
                            exportMessage = "No orders to export for the current filter.";
                            return;
                        }
                    }
                    break;
            }

            byte[] bytes;
            string fileName;
            string mimeType;
            var exportScope = selectedOnly ? "selected" : filter;

            if (format == "csv")
            {
                bytes = AdminSalesOrdersExportBuilder.BuildCsv(
                    tab, exportOrders, exportReturns, exportRefunds, exportScope);
                fileName = AdminSalesOrdersExportBuilder.BuildFileName(
                    tab, "csv", exportScope, selectedOnly);
                mimeType = "text/csv;charset=utf-8";
            }
            else
            {
                bytes = AdminSalesOrdersExportBuilder.BuildExcel(
                    tab, exportOrders, exportReturns, exportRefunds, exportScope);
                fileName = AdminSalesOrdersExportBuilder.BuildFileName(
                    tab, "xlsx", exportScope, selectedOnly);
                mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            }

            await JS.InvokeVoidAsync(
                "adminExport.downloadBytes",
                fileName,
                Convert.ToBase64String(bytes),
                mimeType);

            exportMessage = $"Exported {GetExportCount(tab, exportOrders, exportReturns, exportRefunds)} records.";
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

    private static int GetExportCount(
        string tab,
        List<OrderModel> orders,
        List<OrderReturnModel> returns,
        List<RefundModel> refunds) =>
        tab switch
        {
            "returns" => returns.Count,
            "refunds" => refunds.Count,
            _ => orders.Count
        };

    private async Task<List<OrderModel>> FetchAllOrdersForExportAsync()
    {
        var all = new List<OrderModel>();
        var page = 1;

        while (true)
        {
            var result = await OrderService.GetAllOrders(
                string.IsNullOrEmpty(activeStatus) ? null : activeStatus,
                string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
                page,
                ExportPageSize);

            if (!result.Success || result.Data?.Data is null)
                throw new InvalidOperationException(
                    AdminUiErrorHelper.FromApi(result.Message, "Failed to fetch orders for export."));

            all.AddRange(result.Data.Data);

            if (all.Count >= result.Data.TotalCount || result.Data.Data.Count == 0)
                break;

            page++;
        }

        return all;
    }

    private async Task<List<OrderReturnModel>> FetchAllReturnsForExportAsync()
    {
        var all = new List<OrderReturnModel>();
        var page = 1;

        while (true)
        {
            var result = await OrderReturnService.GetAllReturns(
                string.IsNullOrEmpty(activeReturnStatus) ? null : activeReturnStatus,
                page,
                ExportPageSize);

            if (!result.Success || result.Data?.Data is null)
                throw new InvalidOperationException(
                    AdminUiErrorHelper.FromApi(result.Message, "Failed to fetch returns for export."));

            all.AddRange(result.Data.Data);

            if (all.Count >= result.Data.TotalCount || result.Data.Data.Count == 0)
                break;

            page++;
        }

        return all;
    }

    private async Task<List<RefundModel>> FetchAllRefundsForExportAsync()
    {
        var all = new List<RefundModel>();
        var page = 1;

        while (true)
        {
            var result = await RefundService.GetAllRefunds(
                string.IsNullOrEmpty(activeRefundStatus) ? null : activeRefundStatus,
                page,
                ExportPageSize);

            if (!result.Success || result.Data?.Data is null)
                throw new InvalidOperationException(
                    AdminUiErrorHelper.FromApi(result.Message, "Failed to fetch refunds for export."));

            all.AddRange(result.Data.Data);

            if (all.Count >= result.Data.TotalCount || result.Data.Data.Count == 0)
                break;

            page++;

        }

        return all;
    }
}
