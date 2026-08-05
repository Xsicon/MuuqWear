using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MuuqWear.Model.Customer;
using MuuqWear.Web.Services;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminCustomerComponent
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private bool exportMenuOpen;
    private bool isExporting;
    private string exportMessage = string.Empty;
    private const int ExportPageSize = 100;

    private void ToggleExportMenu() => exportMenuOpen = !exportMenuOpen;

    private async Task ExportAsync(string format)
    {
        if (isExporting)
            return;

        exportMenuOpen = false;
        isExporting = true;
        exportMessage = string.Empty;
        StateHasChanged();

        try
        {
            var exportCustomers = await FetchAllCustomersForExportAsync();
            if (exportCustomers.Count == 0)
            {
                exportMessage = "No customers to export for the current filter.";
                return;
            }

            byte[] bytes;
            string fileName;
            string mimeType;
            var searchFilter = string.IsNullOrWhiteSpace(searchQuery) ? null : searchQuery.Trim();

            if (format == "csv")
            {
                bytes = AdminCustomersExportBuilder.BuildCsv(exportCustomers, searchFilter);
                fileName = AdminCustomersExportBuilder.BuildFileName("csv", searchFilter);
                mimeType = "text/csv;charset=utf-8";
            }
            else
            {
                bytes = AdminCustomersExportBuilder.BuildExcel(exportCustomers, searchFilter);
                fileName = AdminCustomersExportBuilder.BuildFileName("xlsx", searchFilter);
                mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            }

            await JS.InvokeVoidAsync(
                "adminExport.downloadBytes",
                fileName,
                Convert.ToBase64String(bytes),
                mimeType);

            exportMessage = $"Exported {exportCustomers.Count} customer{(exportCustomers.Count == 1 ? "" : "s")}.";
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

    private async Task<List<CustomerModel>> FetchAllCustomersForExportAsync()
    {
        var all = new List<CustomerModel>();
        var page = 1;

        while (true)
        {
            var result = await CustomerService.GetAll(
                string.IsNullOrWhiteSpace(searchQuery) ? null : searchQuery,
                page,
                ExportPageSize,
                string.IsNullOrWhiteSpace(statusFilter) ? null : statusFilter);

            if (!result.Success || result.Data?.Data is null)
                throw new InvalidOperationException(
                    AdminUiErrorHelper.FromApi(result.Message, "Failed to fetch customers for export."));

            all.AddRange(result.Data.Data);

            if (all.Count >= result.Data.TotalCount || result.Data.Data.Count == 0)
                break;

            page++;
        }

        return all;
    }
}
