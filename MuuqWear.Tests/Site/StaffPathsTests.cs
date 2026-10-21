using MuuqWear.Client;
using Xunit;

namespace MuuqWear.Tests.Site;

public class StaffPathsTests
{
    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/orders")]
    [InlineData("/profile")]
    [InlineData("/settings")]
    [InlineData("/account")]
    [InlineData("/community")]
    [InlineData("/help")]
    [InlineData("/careers")]
    [InlineData("/affiliate")]
    [InlineData("/muuqsimo/drop")]
    [InlineData("/shipping-return")]
    [InlineData("/design-history")]
    [InlineData("/order-confirmed/abc")]
    [InlineData("/partner-store")]
    public void StaffAndProfileStayOnTheServer(string path)
    {
        Assert.True(StaffPaths.IsServerOwned(path));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/shop")]
    [InlineData("/shop/apparel")]
    [InlineData("/productdetail/1")]
    [InlineData("/cart")]
    [InlineData("/checkout")]
    [InlineData("/login")]
    [InlineData("/register")]
    [InlineData("/about")]
    [InlineData("/faq")]
    [InlineData("/journal")]
    public void ShopperScreensStayInWasm(string path)
    {
        Assert.False(StaffPaths.IsServerOwned(path));
    }
}
