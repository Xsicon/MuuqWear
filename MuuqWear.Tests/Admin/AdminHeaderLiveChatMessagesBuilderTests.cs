using MuuqWear.Model.Chat;
using MuuqWear.Web.Services;
using Xunit;

namespace MuuqWear.Tests.Admin;

public class AdminHeaderLiveChatMessagesBuilderTests
{
    private static readonly Guid SessionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void CountUnreadSessions_counts_waiting_customer_threads_only()
    {
        var sessions = new List<ChatSessionModel>
        {
            new()
            {
                Id = SessionId,
                Status = "active",
                LastMessageSender = "customer",
                LastActivity = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Status = "active",
                LastMessageSender = "admin",
                LastActivity = DateTime.UtcNow
            }
        };

        Assert.Equal(1, AdminHeaderLiveChatMessagesBuilder.CountUnreadSessions(
            sessions,
            new Dictionary<Guid, DateTime>()));
    }

    [Fact]
    public void IsSessionRead_returns_true_after_mark()
    {
        var session = new ChatSessionModel
        {
            Id = SessionId,
            Status = "active",
            LastMessageSender = "customer",
            LastActivity = DateTime.UtcNow.AddMinutes(-1)
        };

        var readAt = new Dictionary<Guid, DateTime>();
        AdminHeaderLiveChatMessagesBuilder.MarkSessionRead(readAt, SessionId, session.LastActivity);

        Assert.True(AdminHeaderLiveChatMessagesBuilder.IsSessionRead(session, readAt));
    }

    [Fact]
    public void GetSidebarMessageCount_shows_unread_count_for_waiting_threads()
    {
        var session = new ChatSessionModel
        {
            Id = SessionId,
            Status = "active",
            LastMessageSender = "customer",
            LastActivity = DateTime.UtcNow,
            MessageCount = 8,
            UnreadMessageCount = 3
        };

        Assert.Equal(3, AdminHeaderLiveChatMessagesBuilder.GetSidebarMessageCount(
            session,
            new Dictionary<Guid, DateTime>()));
        Assert.True(AdminHeaderLiveChatMessagesBuilder.ShouldHighlightSidebarCount(
            session,
            new Dictionary<Guid, DateTime>()));
    }

    [Fact]
    public void GetSidebarMessageCount_shows_total_after_thread_is_read()
    {
        var session = new ChatSessionModel
        {
            Id = SessionId,
            Status = "active",
            LastMessageSender = "customer",
            LastActivity = DateTime.UtcNow.AddMinutes(-1),
            MessageCount = 8,
            UnreadMessageCount = 3
        };

        var readAt = new Dictionary<Guid, DateTime>();
        AdminHeaderLiveChatMessagesBuilder.MarkSessionRead(readAt, SessionId, session.LastActivity);

        Assert.Equal(8, AdminHeaderLiveChatMessagesBuilder.GetSidebarMessageCount(session, readAt));
        Assert.False(AdminHeaderLiveChatMessagesBuilder.ShouldHighlightSidebarCount(session, readAt));
    }
}
