namespace MuuqWear.Model.AffiliateApplication;

public class UpdateAffiliateTierModel
{
    public string? DisplayName { get; set; }
    public int? ItemsSoldThreshold { get; set; }
    public decimal? CommissionRatePercent { get; set; }
    public decimal? ReferralDiscountPercent { get; set; }
    public decimal? QuarterlyBonusPercent { get; set; }
    public int? MaxAffiliates { get; set; }
    public List<string>? Perks { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}
