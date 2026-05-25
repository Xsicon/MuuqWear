namespace MuuqWear.Model.AffiliateApplication;

/// <summary>
/// Represents performance statistics for a single day
/// </summary>
public class DailyPerformanceModel
{
    /// <summary>
    /// Day number (1-30) for chart display
    /// </summary>
    public int Day { get; set; }

    /// <summary>
    /// Actual date (e.g., 2024-12-01)
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Number of clicks received on this day
    /// </summary>
    public int Clicks { get; set; }

    /// <summary>
    /// Number of conversions (orders) on this day
    /// </summary>
    public int Conversions { get; set; }
}

