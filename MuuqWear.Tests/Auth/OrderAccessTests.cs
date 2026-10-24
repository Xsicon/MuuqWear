using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using MuuqWear.Model.Orders;
using MuuqWear.Web.Helpers;
using Xunit;

namespace MuuqWear.Tests.Auth;

public class OrderAccessTests
{
    [Fact]
    public void ShopperCannotReadAnotherCustomersOrder()
    {
        var order = new OrderModel { CustomerId = "victim-uid", Email = "victim@muuqwear.com" };
        var shopper = Principal("Cookies",
            new Claim("UserId", "attacker-uid"),
            new Claim(ClaimTypes.Email, "attacker@muuqwear.com"),
            new Claim(ClaimTypes.Role, "user"));

        Assert.False(OrderAccess.CanView(order, shopper));
    }

    [Fact]
    public void ShopperCanReadOwnOrderByCustomerId()
    {
        var order = new OrderModel { CustomerId = "uid-1", Email = "a@b.com" };
        var shopper = Principal("Cookies",
            new Claim("UserId", "uid-1"),
            new Claim(ClaimTypes.Email, "a@b.com"));

        Assert.True(OrderAccess.CanView(order, shopper));
    }

    [Fact]
    public void StaffCanReadAnyOrder()
    {
        var order = new OrderModel { CustomerId = "uid-1" };
        var staff = Principal(CookieAuthenticationDefaults.AuthenticationScheme,
            new Claim("UserId", "staff"),
            new Claim(ClaimTypes.Role, "admin"));

        Assert.True(OrderAccess.CanView(order, staff));
        Assert.True(OrderAccess.IsStaff(staff));
    }

    [Fact]
    public void AnonymousCannotReadOrders()
    {
        Assert.False(OrderAccess.CanView(new OrderModel { CustomerId = "uid-1" }, new ClaimsPrincipal()));
    }

    private static ClaimsPrincipal Principal(string authType, params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authType, ClaimTypes.Name, ClaimTypes.Role));
}
