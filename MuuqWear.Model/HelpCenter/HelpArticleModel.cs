using System.Globalization;

namespace MuuqWear.Model.HelpCenter;

public class HelpArticleModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Orders";
    public string Status { get; set; } = "Draft";
    public int ViewCount { get; set; }
    public int HelpfulCount { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? HeroImageUrl { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public List<HelpArticleStepModel> Steps { get; set; } = [];
    public List<HelpArticleCommentModel> Comments { get; set; } = [];
    public int LikeCount { get; set; }
    public int DislikeCount { get; set; }
    public string? MyVote { get; set; }

    public int Views => ViewCount;
    public int Helpful => HelpfulCount > 0 ? HelpfulCount : LikeCount;

    public string LastUpdated =>
        UpdatedAt?.ToString("MMM d, yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
}

public class HelpArticleCommentModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AuthorName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Initials
    {
        get
        {
            var parts = AuthorName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
            return parts.Length > 0
                ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
                : "?";
        }
    }

    public string TimeAgo
    {
        get
        {
            var mins = Math.Max(0, (int)(DateTime.UtcNow - CreatedAt).TotalMinutes);
            if (mins < 60) return $"{mins}m ago";
            var hrs = mins / 60;
            if (hrs < 24) return $"{hrs}h ago";
            return $"{hrs / 24}d ago";
        }
    }
}

public class HelpArticleStepModel
{
    public Guid Id { get; set; }
    public int SortOrder { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class SaveHelpArticleModel
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Orders";
    public string Status { get; set; } = "Draft";
    public string Content { get; set; } = string.Empty;
    public string? HeroImageUrl { get; set; }
    public List<HelpArticleStepModel> Steps { get; set; } = [];
}

public class UpdateHelpArticleStatusModel
{
    public string Status { get; set; } = string.Empty;
}

public class AddArticleCommentModel
{
    public string Body { get; set; } = string.Empty;
}

public class SetArticleVoteModel
{
    public string Vote { get; set; } = string.Empty;
}

public class HelpArticleEngagementModel
{
    public int LikeCount { get; set; }
    public int DislikeCount { get; set; }
    public string? MyVote { get; set; }
    public List<HelpArticleCommentModel> Comments { get; set; } = [];
}

public static class HelpArticleCategories
{
    public static readonly string[] All =
        ["Orders", "Shipping", "Returns", "Payments", "Account", "Product Info"];
}

public static class HelpArticleDisplayStatus
{
    public const string Draft = "Draft";
    public const string Published = "Published";

    public static string FromApi(string? status) =>
        string.Equals(status, "published", StringComparison.OrdinalIgnoreCase)
            ? Published
            : Draft;

    public static string ToApi(string? status) =>
        string.Equals(status, Published, StringComparison.OrdinalIgnoreCase)
            ? "published"
            : "draft";
}
