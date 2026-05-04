namespace MuuqWear.Model.ContentItem;

public class ContentItemModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string Status { get; set; } = "draft";
    public int Views { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsPublished => Status == "published";
    public string? Category { get; set; }  // ← add
    public string? ImageUrl { get; set; }  // ← add
}

public class CreateContentItemModel
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Category { get; set; }  // ← add
    public string? ImageUrl { get; set; }  // ← add
}

public class UpdateContentItemModel
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Category { get; set; }  // ← add
    public string? ImageUrl { get; set; }  // ← add
}

public enum ContentCategory
{
    JournalArticles,
    Events,
    DesignHistory
}
