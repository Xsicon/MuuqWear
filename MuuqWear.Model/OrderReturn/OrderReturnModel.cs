namespace MuuqWear.Model.OrderReturn;

public class OrderReturnModel
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ItemsToReturn { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? CreatedAt { get; set; }
}

// used when user submits return form
public class SubmitReturnModel
{
    public string OrderNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ItemsToReturn { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Comments { get; set; }
}

// used by admin approve/deny
public class UpdateReturnStatusModel
{
    public string Status { get; set; } = string.Empty;
}
