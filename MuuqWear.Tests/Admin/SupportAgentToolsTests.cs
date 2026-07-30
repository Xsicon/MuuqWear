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
    public void SupportTicketTeams_includes_escalations()
    {
        Assert.Contains("Escalations", SupportTicketTeams.All);
        Assert.Equal("Unassigned", SupportTicketTeams.Unassigned);
    }

    [Fact]
    public void SupportTicketAgents_WithCurrentUser_adds_logged_in_agent()
    {
        var agents = SupportTicketAgents.WithCurrentUser("Jordan Lee");

        Assert.Contains("Jordan Lee", agents);
        Assert.Contains("Priya Sharma", agents);
    }

    [Fact]
    public void SupportTicketModel_IsAssignedTo_matches_agent_name()
    {
        var ticket = new SupportTicketModel { AssignedToName = "Priya Sharma" };

        Assert.True(ticket.IsAssignedTo("Priya Sharma"));
        Assert.True(ticket.IsAssignedTo("priya sharma"));
        Assert.False(ticket.IsAssignedTo("Alex Morgan"));
    }

    [Fact]
    public void SupportTicketModel_DisplayReplyCount_prefers_loaded_replies()
    {
        var ticket = new SupportTicketModel
        {
            ReplyCount = 3,
            Replies = [new SupportTicketReplyModel(), new SupportTicketReplyModel()]
        };

        Assert.Equal(2, ticket.DisplayReplyCount);
    }

    [Fact]
    public void HelpArticleModel_Helpful_uses_like_count_when_helpful_count_is_zero()
    {
        var article = new HelpArticleModel { LikeCount = 4, HelpfulCount = 0 };

        Assert.Equal(4, article.Helpful);
    }

    [Fact]
    public void HelpArticleCategories_includes_six_support_topics()
    {
        Assert.Equal(6, HelpArticleCategories.All.Length);
        Assert.Contains("Orders", HelpArticleCategories.All);
        Assert.Contains("Product Info", HelpArticleCategories.All);
    }
}
