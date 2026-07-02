namespace MuuqWear.Model.Refund;

public class RefundModel
{
    public Guid Id { get; set; }
    public string RefundNumber { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Guid? ReturnId { get; set; }
    public string? StripeRefundId { get; set; }
    public string? FailureReason { get; set; }
    public string? Currency { get; set; }
    public string? StripePaymentIntentId { get; set; }
}
