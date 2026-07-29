using MuuqWear.Model.HelpCenter;
using MuuqWear.Web.Components.Pages.AdminComponent.Support;
using Xunit;

namespace MuuqWear.Tests.Admin;

public class SupportAgentToolsTests
{
    [Fact]
    public void SupportCannedReplies_has_six_unique_macros()
    {
        var macros = SupportCannedReplies.All;

        Assert.Equal(6, macros.Count);
        Assert.Equal(6, macros.Select(m => m.Label).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(6, macros.Select(m => m.Text).Distinct(StringComparer.Ordinal).Count());
        Assert.All(macros, m => Assert.False(string.IsNullOrWhiteSpace(m.Label)));
        Assert.All(macros, m => Assert.False(string.IsNullOrWhiteSpace(m.Text)));
    }

    [Fact]
    public void HelpArticleDisplayStatus_maps_api_and_ui_values()
    {
        Assert.Equal("Published", HelpArticleDisplayStatus.FromApi("published"));
        Assert.Equal("Draft", HelpArticleDisplayStatus.FromApi("draft"));
        Assert.Equal("published", HelpArticleDisplayStatus.ToApi("Published"));
        Assert.Equal("draft", HelpArticleDisplayStatus.ToApi("Draft"));
    }

    [Fact]
    public void HelpArticleCategories_includes_six_support_topics()
    {
        Assert.Equal(6, HelpArticleCategories.All.Length);
        Assert.Contains("Orders", HelpArticleCategories.All);
        Assert.Contains("Product Info", HelpArticleCategories.All);
    }
}
