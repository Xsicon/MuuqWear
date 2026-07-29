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
    public void SupportKbArticleCatalog_returns_published_articles_only()
    {
        var articles = SupportKbArticleCatalog.GetPublishedArticles();

        Assert.NotEmpty(articles);
        Assert.All(articles, a => Assert.Equal("Published", a.Status));
        Assert.Equal(
            articles.OrderBy(a => a.Category).ThenBy(a => a.Title).Select(a => a.Id),
            articles.Select(a => a.Id));
    }

    [Fact]
    public void SupportKbArticleCatalog_excludes_draft_articles()
    {
        var articles = SupportKbArticleCatalog.GetPublishedArticles();

        Assert.DoesNotContain(articles, a => a.Title.Contains("delete my account", StringComparison.OrdinalIgnoreCase));
    }
}
