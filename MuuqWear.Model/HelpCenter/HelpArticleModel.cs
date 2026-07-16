namespace MuuqWear.Model.HelpCenter;

public class HelpArticleModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Orders";
    public string Status { get; set; } = "Draft";
    public int Views { get; set; }
    public int Helpful { get; set; }
    public string LastUpdated { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class SaveHelpArticleModel
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Orders";
    public string Status { get; set; } = "Draft";
    public string Content { get; set; } = string.Empty;
    public string LastUpdated { get; set; } = string.Empty;
}
