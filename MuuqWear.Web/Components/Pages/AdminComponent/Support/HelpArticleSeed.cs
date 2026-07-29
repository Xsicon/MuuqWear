using MuuqWear.Model.HelpCenter;

namespace MuuqWear.Web.Components.Pages.AdminComponent.Support;

/// <summary>
/// Reference seed data for SQL migration scripts. Runtime data comes from the Help API.
/// </summary>
public static class HelpArticleSeed
{
    public static readonly string[] Categories = HelpArticleCategories.All;
}

public record CategoryMeta(string Bg, string Text, string Dot, string IconKey);

public static class HelpCategoryMeta
{
    public static CategoryMeta Get(string category) => category switch
    {
        "Shipping" => new("#D1FAE5", "#065F46", "#22C55E", "truck"),
        "Returns" => new("#FEE2E2", "#991B1B", "#EF4444", "return"),
        "Payments" => new("#EDE9FE", "#5B21B6", "#8B5CF6", "card"),
        "Account" => new("#FEF3C7", "#92400E", "#F59E0B", "settings"),
        "Product Info" => new("#F0FDF4", "#166534", "#4ADE80", "help"),
        _ => new("#DBEAFE", "#1D4ED8", "#3B82F6", "package")
    };
}
