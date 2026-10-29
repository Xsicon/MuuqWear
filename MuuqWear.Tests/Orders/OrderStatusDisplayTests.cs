using MuuqWear.Model.Orders;
using Xunit;

namespace MuuqWear.Tests.Orders;

public class OrderStatusDisplayTests
{
    [Theory]
    [InlineData(null, "Pending")]
    [InlineData("", "Pending")]
    [InlineData("delivered", "Delivered")]
    [InlineData("In Transit", "In Transit")]
    [InlineData("shipped", "In Transit")]
    [InlineData("cancelled", "Cancelled")]
    [InlineData("canceled", "Cancelled")]
    [InlineData("pending", "Pending")]
    public void Format_maps_known_statuses(string? status, string expected)
    {
        Assert.Equal(expected, OrderStatusDisplay.Format(status));
    }

    [Theory]
    [InlineData("delivered", "is-delivered")]
    [InlineData("in transit", "is-transit")]
    [InlineData("shipped", "is-transit")]
    [InlineData("cancelled", "is-cancelled")]
    [InlineData("pending", "is-pending")]
    [InlineData("weird", "")]
    public void CssClass_maps_known_statuses(string? status, string expected)
    {
        Assert.Equal(expected, OrderStatusDisplay.CssClass(status));
    }
}
