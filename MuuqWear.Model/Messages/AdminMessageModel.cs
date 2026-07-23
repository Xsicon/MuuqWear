namespace MuuqWear.Model.Messages;

public static class AdminMessageKind
{
    public const string Note = "note";
    public const string LiveChat = "live_chat";
}

/// <summary>
/// Admin header message item (customer internal notes, live chat, etc.).
/// </summary>
public class AdminMessageModel
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = AdminMessageKind.Note;
    public Guid CustomerId { get; set; }
    public Guid? ChatSessionId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Preview { get; set; } = string.Empty;
    public string AuthorLabel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public string Link { get; set; } = string.Empty;

    public bool IsLiveChat =>
        Kind.Equals(AdminMessageKind.LiveChat, StringComparison.OrdinalIgnoreCase)
        || ChatSessionId.HasValue;

    public string TimeDisplay
    {
        get
        {
            var created = CreatedAt.Kind switch
            {
                DateTimeKind.Utc => CreatedAt,
                DateTimeKind.Local => CreatedAt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc)
            };

            var diff = DateTime.UtcNow - created;
            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hour ago";
            return $"{(int)diff.TotalDays} days ago";
        }
    }
}
