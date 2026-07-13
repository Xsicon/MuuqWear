namespace MuuqWear.Model.AffiliateApplication;

public class AffiliateTierModel
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int ItemsSoldThreshold { get; set; }
    public decimal CommissionRatePercent { get; set; }
    public decimal ReferralDiscountPercent { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string ThresholdLabel => ItemsSoldThreshold <= 0
        ? "Starting tier"
        : $"{ItemsSoldThreshold} items sold to unlock";

    public string CommissionPerTenLabel =>
        $"{CommissionRatePercent:0.#}% commission per 10 items";

    public string ReferralDiscountLabel =>
        $"{ReferralDiscountPercent:0.#}%";

    public string ItemsDetailLabel => ItemsSoldThreshold <= 0
        ? "Starting tier"
        : $"{ItemsSoldThreshold}+ items";

    public decimal CommissionExampleAmount(decimal orderTotal = 5500m) =>
        Math.Round(orderTotal * CommissionRatePercent / 100m, 2);
}
