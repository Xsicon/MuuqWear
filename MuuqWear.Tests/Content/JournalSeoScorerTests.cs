using MuuqWear.Application.Content;
using MuuqWear.Model.ContentItem;
using Xunit;

namespace MuuqWear.Tests.Content;

public class JournalSeoScorerTests
{
    [Fact]
    public void Score_uses_stored_seo_title_and_excerpt()
    {
        var item = new ContentItemModel
        {
            Title = "Short",
            SeoTitle = "A Well-Crafted SEO Title Between Fifty And Sixty Chars Long | Muuqwear",
            Excerpt = new string('a', 130),
            Content = string.Join(' ', Enumerable.Repeat("word", 400)),
            Category = "Design",
            ImageUrl = "https://cdn.example.com/photo.jpg"
        };

        var withStoredFields = JournalSeoScorer.Score(item);

        item.SeoTitle = null;
        item.Excerpt = null;
        var withoutStoredFields = JournalSeoScorer.Score(item);

        Assert.True(withStoredFields > withoutStoredFields);
    }

    [Fact]
    public void BuildSlug_normalizes_title()
    {
        Assert.Equal("the-nordic-rune-sweater", JournalSeoScorer.BuildSlug("The Nordic Rune Sweater"));
        Assert.Equal("tech-jacket-v2", JournalSeoScorer.BuildSlug("Tech Jacket v2"));
    }

    [Fact]
    public void Score_returns_zero_for_empty_input()
    {
        var score = JournalSeoScorer.Score(new JournalSeoInput(null, null, null, null, null));
        Assert.Equal(0, score);
    }

    [Fact]
    public void EstimateReadTimeMinutes_returns_at_least_one()
    {
        Assert.Equal(1, JournalSeoScorer.EstimateReadTimeMinutes(null));
        Assert.True(JournalSeoScorer.EstimateReadTimeMinutes(string.Join(' ', Enumerable.Repeat("word", 500))) >= 3);
    }
}
