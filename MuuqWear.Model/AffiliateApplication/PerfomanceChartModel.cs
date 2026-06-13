namespace MuuqWear.Model.AffiliateApplication;

/// <summary>
/// Contains 30 days of performance data for charting
/// </summary>
public class PerformanceChartModel
{
    /// <summary>
    /// List of daily statistics (30 days)
    /// </summary>
    public List<DailyPerformanceModel> DailyStats { get; set; } = new();
}
