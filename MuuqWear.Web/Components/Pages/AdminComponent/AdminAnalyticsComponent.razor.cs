using Microsoft.JSInterop;
using MuuqWear.Model.AffiliatePerfomanceModel;
using MuuqWear.Model.RevenueOverTime;
using MuuqWear.Model.TopSellingProduct;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminAnalyticsComponent
{
    private bool isLoading = false;
    private RevenueOverTimeModel? revenue = null;
    private List<TopSellingProductModel>? topProducts = null;
    private List<AffiliatePerformanceModel>? affiliatePerformance = null;
    private bool pendingChartRender = false;


    protected override async Task OnInitializedAsync()
    {
        await LoadAllData();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Render the chart only when we have data AND the canvas is in the DOM
        if (pendingChartRender && revenue is not null && revenue.DailyRevenue.Any())
        {
            pendingChartRender = false;
            await RenderChart();
        }
    }

    private async Task LoadAllData()
    {
        isLoading = true;
        StateHasChanged();

        var revenueTask = AnalyticsService.GetRevenue();
        var productsTask = AnalyticsService.GetTopProducts(5);
        var affiliatesTask = AnalyticsService.GetAffiliatePerformance();

        await Task.WhenAll(revenueTask, productsTask, affiliatesTask);

        revenue = revenueTask.Result.Success
            ? revenueTask.Result.Data
            : null;

        topProducts = productsTask.Result.Success && productsTask.Result.Data != null
            ? productsTask.Result.Data
            : new List<TopSellingProductModel>();

        affiliatePerformance = affiliatesTask.Result.Success && affiliatesTask.Result.Data != null
            ? affiliatesTask.Result.Data
            : new List<AffiliatePerformanceModel>();

        isLoading = false;

        // Signal that we want the chart to render after the next render cycle
        pendingChartRender = true;

        StateHasChanged();
    }

    private async Task RenderChart()
    {
        if (revenue is null || !revenue.DailyRevenue.Any()) return;

        try
        {
            // Format labels: "Mar 1", "Mar 2", etc.
            var labels = revenue.DailyRevenue
                .Select(d => d.Day.ToString("MMM d"))
                .ToArray();

            var values = revenue.DailyRevenue
                .Select(d => (double)d.Revenue)
                .ToArray();

            await JS.InvokeVoidAsync(
                "muuqAnalyticsChart.render",
                "revenue-chart",
                labels,
                values);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Analytics] Chart render failed: {ex.Message}");
        }
    }
    private async Task HandleRefresh()
    {
        await LoadAllData();
    }

    private void HandleExport()
    {
        // Wired in Stage 7
    }

    // =============================================
    // HELPERS
    // =============================================

    private static string FormatNumber(decimal value)
    {
        // 12345.67 → "12,345.67"
        // 124567 → "124,567"
        if (value % 1 == 0)
            return value.ToString("N0");
        return value.ToString("N2");
    }

    private static string GetTierLabel(string tier) => tier switch
    {
        "gold" => "Gold Tier",
        "silver" => "Silver Tier",
        "bronze" => "Bronze Tier",
        _ => char.ToUpper(tier[0]) + tier.Substring(1) + " Tier"
    };

    private static string GetTierIcon(string tier) => tier switch
    {
        "gold" => "★",
        "silver" => "✦",
        "bronze" => "◆",
        _ => "•"
    };

    private static string GetTierIconClass(string tier) => $"tier-icon--{tier}";
}