namespace MuuqWear.Model.RevenueOverTime;

public class RevenueOverTimeModel
{
    public List<DailyRevenueModel> DailyRevenue { get; set; } = new();
    public decimal CurrentTotal { get; set; }
    public decimal PreviousTotal { get; set; }
    public decimal PercentChange { get; set; }
    public bool IsUp { get; set; }
}

public class DailyRevenueModel
{
    public DateTime Day { get; set; }
    public decimal Revenue { get; set; }
}
