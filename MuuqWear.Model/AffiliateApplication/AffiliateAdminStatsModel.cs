namespace MuuqWear.Model.AffiliateApplication;

public class AffiliateAdminStatsModel
{
    public int PendingApplications { get; set; }
    public int WaitlistedApplications { get; set; }
    public int ApplicationsThisMonth { get; set; }
    public int ActiveAffiliates { get; set; }
    public int InactiveAffiliates { get; set; }
    public Dictionary<string, int> AffiliatesByTier { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public decimal TotalCommissionsPaid { get; set; }
    public int TotalItemsSold { get; set; }
    public decimal PendingPayoutAmount { get; set; }
    public int PendingPayoutCount { get; set; }
    public decimal ProcessedThisMonthAmount { get; set; }
    public int ProcessedThisMonthCount { get; set; }
    public decimal TotalDisbursedYtd { get; set; }
}
