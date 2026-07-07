namespace MuuqWear.Model.Messages;

/// <summary>
/// Admin header message item (customer internal notes, etc.).
/// </summary>
public class AdminMessageModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Preview { get; set; } = string.Empty;
    public string AuthorLabel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public string Link { get; set; } = string.Empty;

    public string TimeDisplay
    {
        get
        {
            var created = CreatedAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc)
                : CreatedAt.ToUniversalTime();

            var diff = DateTime.UtcNow - created;
            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hour ago";
            return $"{(int)diff.TotalDays} days ago";
        }
    }
}
