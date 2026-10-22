using MuuqWear.Application.Shared;
using Xunit;

namespace MuuqWear.Tests.Admin;

public class AdminDirectoryFilterTests
{
    [Theory]
    [InlineData("buyer@gmail.com", true)]
    [InlineData("ops@muuqwear.com", false)]
    [InlineData("sandbox.ok4.1@kobneti.com", false)]
    [InlineData("buyer1@demo.muuqwear.test", false)]
    public void Shopper_email_filter(string email, bool expected) =>
        Assert.Equal(expected, AdminDirectoryFilter.IsShopperEmail(email));

    [Fact]
    public void Money_uses_dollar() =>
        Assert.Equal("$22.00", AdminMoney.Format(22m));

    [Fact]
    public void Relative_time_pluralizes_hours()
    {
        var text = AdminRelativeTime.Format(DateTime.UtcNow.AddHours(-13));
        Assert.Contains("hours ago", text);
    }
}
