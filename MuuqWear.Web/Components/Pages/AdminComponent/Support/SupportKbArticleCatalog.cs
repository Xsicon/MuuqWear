using MuuqWear.Model.HelpCenter;

namespace MuuqWear.Web.Components.Pages.AdminComponent.Support;

/// <summary>
/// Read-only KB catalog for agent quick-search. Uses seed data until Phase 3 API persistence.
/// </summary>
public static class SupportKbArticleCatalog
{
    public static IReadOnlyList<HelpArticleModel> GetPublishedArticles() =>
        HelpArticleSeed.CreateInitialArticles()
            .Where(a => a.Status == "Published")
            .OrderBy(a => a.Category)
            .ThenBy(a => a.Title)
            .ToList();
}
