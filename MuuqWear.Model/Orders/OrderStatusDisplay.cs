namespace MuuqWear.Model.Orders;

public static class OrderStatusDisplay
{
    public static string Format(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return "Pending";

        var value = status.Trim().ToLowerInvariant();
        return value switch
        {
            "in transit" or "transit" or "shipped" => "In Transit",
            "delivered" => "Delivered",
            "cancelled" or "canceled" => "Cancelled",
            "pending" => "Pending",
            _ => char.ToUpperInvariant(status.Trim()[0]) + status.Trim()[1..]
        };
    }

    public static string CssClass(string? status)
    {
        var value = status?.Trim().ToLowerInvariant();
        return value switch
        {
            "delivered" => "is-delivered",
            "in transit" or "transit" or "shipped" => "is-transit",
            "cancelled" or "canceled" => "is-cancelled",
            "pending" => "is-pending",
            _ => ""
        };
    }
}
