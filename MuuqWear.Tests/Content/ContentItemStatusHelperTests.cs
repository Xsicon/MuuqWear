using MuuqWear.Application.Content;
using MuuqWear.Model.ContentItem;
using Xunit;

namespace MuuqWear.Tests.Content;

public class ContentItemStatusHelperTests
{
    [Theory]
    [InlineData("published", "Published", "content-status-badge--published")]
    [InlineData("scheduled", "Scheduled", "content-status-badge--scheduled")]
    [InlineData("archived", "Archived", "content-status-badge--archived")]
    [InlineData("draft", "Draft", "content-status-badge--draft")]
    public void Status_helpers_map_known_values(string status, string label, string badgeClass)
    {
        var item = new ContentItemModel { Status = status };

        Assert.Equal(label, ContentItemStatusHelper.GetDisplayLabel(item));
        Assert.Equal(badgeClass, ContentItemStatusHelper.GetStatusBadgeClass(item));
    }

    [Fact]
    public void CountByStatus_counts_matching_items()
    {
        var items = new[]
        {
            new ContentItemModel { Status = "draft" },
            new ContentItemModel { Status = "scheduled" },
            new ContentItemModel { Status = "published" }
        };

        Assert.Equal(1, ContentItemStatusHelper.CountByStatus(items, "scheduled"));
    }

    [Fact]
    public void IsDraftStatus_only_matches_draft()
    {
        Assert.True(ContentItemStatusHelper.IsDraftStatus("draft"));
        Assert.False(ContentItemStatusHelper.IsDraftStatus("scheduled"));
        Assert.False(ContentItemStatusHelper.IsDraftStatus("published"));
    }

    [Fact]
    public void CanPreviewOnSite_only_allows_published()
    {
        var published = new ContentItemModel { Status = "published" };
        var scheduled = new ContentItemModel { Status = "scheduled" };

        Assert.True(ContentItemStatusHelper.CanPreviewOnSite(published));
        Assert.False(ContentItemStatusHelper.CanPreviewOnSite(scheduled));
    }
}
