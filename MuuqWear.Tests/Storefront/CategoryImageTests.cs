using MuuqWear.Client.Pages;
using Xunit;

namespace MuuqWear.Tests.Storefront;

public class CategoryImageTests
{
    [Theory]
    [InlineData("mens")]
    [InlineData("Mens")]
    [InlineData("MENS")]
    [InlineData(" accessories ")]
    [InlineData("Kids")]
    [InlineData("womens")]
    [InlineData("outerwear")]
    public void ResolveCategoryImage_matches_known_categories_case_insensitively(string name)
    {
        var url = HomeView.ResolveCategoryImage(name);
        Assert.DoesNotContain("1441986300917", url);
        Assert.StartsWith("https://images.unsplash.com/photo-", url);
    }

    [Fact]
    public void ResolveCategoryImage_uses_fallback_for_unknown()
    {
        var url = HomeView.ResolveCategoryImage("unknown");
        Assert.Contains("1441986300917", url);
    }

    [Fact]
    public void ResolveCategoryImage_uses_distinct_urls_per_category()
    {
        var urls = new[] { "mens", "womens", "kids", "accessories", "outerwear" }
            .Select(HomeView.ResolveCategoryImage)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Equal(5, urls.Count);
    }
}
