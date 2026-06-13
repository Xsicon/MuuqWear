namespace MuuqWear.Model.AffiliateApplication;

public class AffiliateStatusModel
{
    public string ApplicationStatus { get; set; } = "not_applied";
    public string Tier { get; set; } = "none";
    public int ItemsSold { get; set; }
    public decimal CommissionEarned { get; set; }
    public DateTime? SubmittedAt { get; set; }
}

