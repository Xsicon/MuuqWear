namespace MuuqWear.Model.AffiliateApplication;

public class UpdateAffiliateTierModel
{
    public string? DisplayName { get; set; }
    public int? ItemsSoldThreshold { get; set; }
    public decimal? CommissionRatePercent { get; set; }
    public decimal? ReferralDiscountPercent { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}
