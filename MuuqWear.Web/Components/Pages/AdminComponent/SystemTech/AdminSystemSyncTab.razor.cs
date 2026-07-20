using Microsoft.AspNetCore.Components;
using MuuqWear.Application.Services.AdminSystemService;
using MuuqWear.Web.Services;

namespace MuuqWear.Web.Components.Pages.AdminComponent.SystemTech;

public partial class AdminSystemSyncTab
{
    [Inject] private IAdminSystemService AdminSystemService { get; set; } = default!;
    [Inject] private AdminBadgeCountsCacheService BadgeCountsCache { get; set; } = default!;
    [Inject] private AdminLowStockCacheService LowStockCache { get; set; } = default!;
    [Inject] private AdminContentCountsCacheService ContentCountsCache { get; set; } = default!;

    private sealed record SyncTool(string Id, string Label, string Description);

    private readonly SyncTool[] syncTools =
    [
        new("stripe-orders", "Resync Stripe Orders", "Re-import all orders from Stripe"),
        new("affiliate-commissions", "Recalculate Affiliate Commissions", "Recalculate all pending commissions"),
        new("inventory-erp", "Sync Inventory from ERP", "Update stock levels from ERP system"),
        new("clear-cache", "Clear Cache", "Clear all application caches")
    ];

    private bool isRunning;
    private string? runningToolId;
    private string? toast;
    private bool toastIsError;

    private async Task RunSync(string toolId)
    {
        isRunning = true;
        runningToolId = toolId;
        toast = null;
        toastIsError = false;
        StateHasChanged();

        try
        {
            if (toolId == "clear-cache")
            {
                toast = RunClearCache();
                toastIsError = false;
            }
            else
            {
                var result = await AdminSystemService.RunSyncAsync(toolId);
                toastIsError = !result.Success;

                if (result.Success && result.Data != null)
                {
                    var affected = result.Data.RecordsAffected.HasValue
                        ? $" ({result.Data.RecordsAffected} records)"
                        : string.Empty;
                    toast = string.IsNullOrWhiteSpace(result.Data.Message)
                        ? $"{syncTools.First(t => t.Id == toolId).Label} completed{affected}."
                        : result.Data.Message;
                }
                else
                {
                    toast = AdminUiErrorHelper.FromApi(
                        result.Message,
                        $"{syncTools.First(t => t.Id == toolId).Label} failed.");
                }
            }
        }
        catch (Exception ex)
        {
            toastIsError = true;
            toast = AdminUiErrorHelper.FromException(ex);
        }
        finally
        {
            isRunning = false;
            runningToolId = null;
            StateHasChanged();
        }
    }

    private string RunClearCache()
    {
        BadgeCountsCache.Invalidate();
        LowStockCache.Invalidate();
        ContentCountsCache.Invalidate();
        return "Application caches cleared.";
    }
}
