namespace MuuqWear.Model.AffiliatePerfomanceModel;

public class AffiliatePerformanceModel
{
    public string Tier { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public decimal Commission { get; set; }
    public int AffiliateCount { get; set; }
}
