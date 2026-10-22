namespace MuuqWear.Application.Shared;

public static class AdminRelativeTime
{
    public static string Format(DateTime? utc)
    {
        if (utc is null)
            return "Recently";

        var diff = DateTime.UtcNow - utc.Value.ToUniversalTime();
        if (diff.TotalMinutes < 1)
            return "Just now";
        if (diff.TotalMinutes < 60)
        {
            var minutes = Math.Max(1, (int)diff.TotalMinutes);
            return $"{minutes} minute{(minutes == 1 ? "" : "s")} ago";
        }

        if (diff.TotalHours < 24)
        {
            var hours = Math.Max(1, (int)diff.TotalHours);
            return $"{hours} hour{(hours == 1 ? "" : "s")} ago";
        }

        if (diff.TotalDays < 7)
        {
            var days = Math.Max(1, (int)diff.TotalDays);
            return $"{days} day{(days == 1 ? "" : "s")} ago";
        }

        return utc.Value.ToLocalTime().ToString("MMM d, yyyy");
    }
}
