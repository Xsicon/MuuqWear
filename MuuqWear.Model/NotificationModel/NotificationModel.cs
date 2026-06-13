namespace MuuqWear.Model.NotificationModel;

public class NotificationModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; } = false;

    //  computed — human readable time
    public string TimeDisplay
    {
        get
        {
            var diff = DateTime.UtcNow - CreatedAt;
            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hour ago";
            return $"{(int)diff.TotalDays} days ago";
        }
    }
}

public static class NotificationType
{
    public const string Order = "order";
    public const string Ticket = "ticket";
    public const string Return = "return";
    public const string LowStock = "low_stock";
}
