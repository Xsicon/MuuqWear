namespace MuuqWear.Model.AffiliateApplication;

public class ProcessAllAffiliatePayoutsModel
{
    public string? PaymentMethod { get; set; }
    public string? AdminNotes { get; set; }
}

public class ProcessAllAffiliatePayoutsResultModel
{
    public int ProcessedCount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<Guid> PayoutIds { get; set; } = new();
}
