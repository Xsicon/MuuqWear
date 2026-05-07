namespace MuuqWear.Model.AdminSettingsUser;

public class AdminSettingsUserModel
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public bool IsDeleted { get; set; }

    //  computed — initials for avatar
    // e.g. "Jordan Doe" → "JD"
    public string Initials
    {
        get
        {
            if (string.IsNullOrEmpty(FullName)) return "?";
            var parts = FullName.Trim().Split(' ',
                StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
                : parts[0][0].ToString().ToUpper();
        }
    }

    //  computed — human readable last active
    public string LastActiveDisplay
    {
        get
        {
            if (!LastActiveAt.HasValue) return "Never";
            var diff = DateTime.UtcNow - LastActiveAt.Value;
            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 2) return "Yesterday";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return LastActiveAt.Value.ToString("MMM dd, yyyy");
        }
    }
}

public class InviteAdminSettingsUserModel
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UpdateAdminSettingsUserModel
{
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class SupabaseHealthModel
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
}

public class IntegrationModel
{
    public string Name { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public bool IsHealthy { get; set; } = true;
    public bool IsLoading { get; set; } = false;
    public bool IsStatic { get; set; } = false;
}
