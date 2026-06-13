namespace MuuqWear.Model.Orders;

public class BulkUpdateOrderStatusModel
{
    public List<Guid> OrderIds { get; set; } = new();
    public string Status { get; set; } = string.Empty;
}
