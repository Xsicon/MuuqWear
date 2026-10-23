using MuuqWear.Application.Shared;
using Xunit;

namespace MuuqWear.Tests.Auth;

public class ShopperAccountTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("user")]
    [InlineData("User")]
    [InlineData("customer")]
    [InlineData("Customer")]
    public void Storefront_accepts_shopper_roles(string? role) =>
        Assert.True(ShopperAccount.CanUseStorefront(role));

    [Theory]
    [InlineData("admin")]
    [InlineData("operations_manager")]
    [InlineData("support_team")]
    public void Storefront_rejects_staff_roles(string role) =>
        Assert.False(ShopperAccount.CanUseStorefront(role));
}
