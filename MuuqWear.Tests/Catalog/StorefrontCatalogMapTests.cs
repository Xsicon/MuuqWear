using MuuqWear.Application.Catalog;
using Xunit;

namespace MuuqWear.Tests.Catalog;

public class StorefrontCatalogMapTests
{
    [Fact]
    public void PriceIsDollarsFromIntegerCents()
    {
        var product = StorefrontCatalogMap.Product(
            "366f84ba-250b-4d76-ab0f-89e36890ae58",
            "Leather Keychain",
            2200,
            "published",
            new Dictionary<string, long> { ["One Size"] = 27 },
            new[] { "products/keychain.webp" },
            new[] { "Black" },
            new[] { "accessories" },
            "61432954-27a5-46d3-a919-945918970bd0",
            "Premium leather key fob",
            "KEY");

        Assert.Equal(22.00m, product.Price);
        Assert.True(product.IsActive);
        Assert.Equal(27, product.Stock);
        Assert.Contains("alt=media", product.ImageUrl);
        Assert.DoesNotContain("supabase.co", product.ImageUrl);
    }
}
