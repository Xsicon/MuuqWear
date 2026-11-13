using MuuqWear.Application.Shared;
using Xunit;

namespace MuuqWear.Tests.Site;

public class ShopperDisplayNameTests
{
    [Fact]
    public void Email_is_not_a_display_name()
    {
        Assert.False(ShopperDisplayName.IsPersonName("gaarillc12@gmail.com"));
        Assert.Equal("", ShopperDisplayName.Choose("gaarillc12@gmail.com", null, "gaarillc12@gmail.com"));
    }

    [Fact]
    public void Stored_full_name_wins_over_an_email()
    {
        Assert.Equal(
            "Ada Mohamed",
            ShopperDisplayName.Choose("ada@muuqwear.com", "Ada Mohamed", "ada@muuqwear.com"));
    }

    [Fact]
    public void Missing_name_does_not_render_the_email()
    {
        Assert.Equal("", ShopperDisplayName.Choose(null, " ", "shopper@muuqwear.com"));
        Assert.Equal("there", ShopperDisplayName.Greeting(null));
        Assert.Equal("there", ShopperDisplayName.Greeting("shopper@muuqwear.com"));
        Assert.Equal("Ada", ShopperDisplayName.Greeting("Ada Mohamed"));
    }
}
