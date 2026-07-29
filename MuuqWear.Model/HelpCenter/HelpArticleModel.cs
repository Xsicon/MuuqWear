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

    public int Views => ViewCount;
    public int Helpful => HelpfulCount;

    public string LastUpdated =>
        UpdatedAt?.ToString("MMM d, yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
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
