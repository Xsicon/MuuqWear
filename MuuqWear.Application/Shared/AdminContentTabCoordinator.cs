using MuuqWear.Model.ContentItem;

namespace MuuqWear.Application.Shared;

/// <summary>
/// Syncs Content view switches between the sidebar (layout island)
/// and AdminContentComponent (page island) without a full page reload.
/// </summary>
public class AdminContentTabCoordinator
{
    public event Action<string>? ViewChanged;

    public void NotifyViewChanged(string view)
    {
        ViewChanged?.Invoke(view);
    }

    public static string NormalizeView(string? view) =>
        view?.ToLowerInvariant() switch
        {
            "events" => "events",
            "design-history" => "design-history",
            "vote" => "vote",
            "media" => "media",
            _ => "journal"
        };

    public static bool IsContentCategoryView(string view) =>
        NormalizeView(view) is "journal" or "events" or "design-history";

    public static ContentCategory ViewToCategory(string view) =>
        NormalizeView(view) switch
        {
            "events" => ContentCategory.Events,
            "design-history" => ContentCategory.DesignHistory,
            _ => ContentCategory.JournalArticles
        };

    public static string CategoryToView(ContentCategory category) =>
        category switch
        {
            ContentCategory.Events => "events",
            ContentCategory.DesignHistory => "design-history",
            _ => "journal"
        };
}
