using MuuqWear.Model.Chat;
using MuuqWear.Model.Messages;
using MuuqWear.Web.Constants;
using MuuqWear.Web.Helpers;

namespace MuuqWear.Web.Services;

/// <summary>
/// Builds admin header messages from active live chat sessions.
/// </summary>
public static class AdminHeaderLiveChatMessagesBuilder
{
    public const int MaxItems = 15;

    public static bool CanSeeLiveChat(string? role) =>
        AdminPortalRoles.CanAccess(role, AdminPortalSection.Support);

    public static bool IsWaitingForAdmin(ChatSessionModel session) =>
        session.Status == "active"
        && session.LastMessageSender is "customer" or null;

    public static bool IsSessionRead(
        ChatSessionModel session,
        IReadOnlyDictionary<Guid, DateTime> readAtBySessionId)
    {
        if (!readAtBySessionId.TryGetValue(session.Id, out var readAt))
            return false;

        return AdminDateTimeHelper.ToUtc(session.LastActivity)
               <= AdminDateTimeHelper.ToUtc(readAt);
    }

    public static int CountUnreadSessions(
        IEnumerable<ChatSessionModel> sessions,
        IReadOnlyDictionary<Guid, DateTime> readAtBySessionId) =>
        sessions.Count(session =>
            IsWaitingForAdmin(session)
            && !IsSessionRead(session, readAtBySessionId));

    public static bool ShouldHighlightSidebarCount(
        ChatSessionModel session,
        IReadOnlyDictionary<Guid, DateTime> readAtBySessionId) =>
        IsWaitingForAdmin(session) && !IsSessionRead(session, readAtBySessionId);

    public static int GetSidebarMessageCount(
        ChatSessionModel session,
        IReadOnlyDictionary<Guid, DateTime> readAtBySessionId)
    {
        if (ShouldHighlightSidebarCount(session, readAtBySessionId))
            return Math.Max(session.UnreadMessageCount, 1);

        return session.MessageCount;
    }

    public static void MarkSessionRead(
        IDictionary<Guid, DateTime> readAtBySessionId,
        Guid sessionId,
        DateTime lastActivity)
    {
        var readAt = AdminDateTimeHelper.ToUtc(lastActivity);
        if (readAtBySessionId.TryGetValue(sessionId, out var existing)
            && AdminDateTimeHelper.ToUtc(existing) > readAt)
            readAt = AdminDateTimeHelper.ToUtc(existing);

        readAtBySessionId[sessionId] = readAt;
    }

    public static void MarkAllSessionsRead(
        IEnumerable<ChatSessionModel> sessions,
        IDictionary<Guid, DateTime> readAtBySessionId)
    {
        foreach (var session in sessions.Where(IsWaitingForAdmin))
            MarkSessionRead(readAtBySessionId, session.Id, session.LastActivity);
    }

    public static List<AdminMessageModel> FromSessions(
        IEnumerable<ChatSessionModel> sessions,
        IReadOnlyDictionary<Guid, DateTime> readAtBySessionId) =>
        sessions
            .Where(IsWaitingForAdmin)
            .OrderByDescending(session => session.LastActivity)
            .Take(MaxItems)
            .Select(session => ToMessage(session, readAtBySessionId))
            .ToList();

    private static AdminMessageModel ToMessage(
        ChatSessionModel session,
        IReadOnlyDictionary<Guid, DateTime> readAtBySessionId)
    {
        var createdAt = AdminDateTimeHelper.ToUtc(session.LastActivity);
        var name = string.IsNullOrWhiteSpace(session.CustomerName)
            ? "Customer"
            : session.CustomerName.Trim();

        return new AdminMessageModel
        {
            Id = session.Id,
            Kind = AdminMessageKind.LiveChat,
            ChatSessionId = session.Id,
            CustomerName = name,
            Preview = TruncatePreview(session.LastMessagePreview),
            AuthorLabel = "Live chat",
            CreatedAt = createdAt,
            IsRead = IsSessionRead(session, readAtBySessionId),
            Link = $"/admin/support?tab=live-chat&sessionId={session.Id}"
        };
    }

    private static string TruncatePreview(string? value, int maxLength = 120)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "New chat message";

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength].TrimEnd() + "…";
    }
}
