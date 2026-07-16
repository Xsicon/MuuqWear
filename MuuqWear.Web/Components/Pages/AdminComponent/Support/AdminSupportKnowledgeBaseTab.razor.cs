using Microsoft.AspNetCore.Components;
using MuuqWear.Model.HelpCenter;

namespace MuuqWear.Web.Components.Pages.AdminComponent.Support;

public partial class AdminSupportKnowledgeBaseTab : IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    private List<HelpArticleModel> articles = HelpArticleSeed.CreateInitialArticles();
    private string search = string.Empty;
    private string catFilter = "All";
    private string statusFilter = "All";
    private bool panelOpen;
    private HelpArticleModel? editingArticle;
    private HelpArticleModel? deleteTarget;
    private string? toast;
    private System.Threading.Timer? toastTimer;

    private string draftTitle = string.Empty;
    private string draftCategory = "Orders";
    private string draftStatus = "Draft";
    private string draftContent = string.Empty;

    private static readonly string[] CategoryPills = ["All", .. HelpArticleSeed.Categories];
    private static readonly string[] StatusPills = ["All", "Published", "Draft"];

    private IEnumerable<HelpArticleModel> FilteredArticles
    {
        get
        {
            var q = search.Trim().ToLowerInvariant();
            return articles.Where(a =>
                (string.IsNullOrEmpty(q) ||
                 a.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                 a.Content.Contains(q, StringComparison.OrdinalIgnoreCase)) &&
                (catFilter == "All" || a.Category == catFilter) &&
                (statusFilter == "All" || a.Status == statusFilter));
        }
    }

    private IEnumerable<(string Label, string Value, string Color, string IconSvg)> StatCards
    {
        get
        {
            var published = articles.Count(a => a.Status == "Published");
            var views = articles.Sum(a => a.Views);
            return
            [
                ("Total Articles", articles.Count.ToString(), "#1E2A47",
                    """<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#1E2A47" stroke-width="2"><path d="M12 7v14"/><path d="M3 18a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1h5a4 4 0 0 1 4 4 4 4 0 0 1 4-4h5a1 1 0 0 1 1 1v13a1 1 0 0 1-1 1h-6a3 3 0 0 0-3 3 3 3 0 0 0-3-3z"/></svg>"""),
                ("Published", published.ToString(), "#22C55E",
                    """<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#22C55E" stroke-width="2"><circle cx="12" cy="12" r="10"/><path d="M12 2a14.5 14.5 0 0 0 0 20 14.5 14.5 0 0 0 0-20"/><path d="M2 12h20"/></svg>"""),
                ("Drafts", (articles.Count - published).ToString(), "#F59E0B",
                    """<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#F59E0B" stroke-width="2"><path d="M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z"/><path d="M14 2v4a2 2 0 0 0 2 2h4"/></svg>"""),
                ("Total Views", views.ToString("N0"), "#6366F1",
                    """<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#6366F1" stroke-width="2"><path d="M2.062 12.348a1 1 0 0 1 0-.696 10.75 10.75 0 0 1 19.876 0 1 1 0 0 1 0 .696 10.75 10.75 0 0 1-19.876 0"/><circle cx="12" cy="12" r="3"/></svg>""")
            ];
        }
    }

    private void ToggleCatFilter(string cat) =>
        catFilter = catFilter == cat ? "All" : cat;

    private void OpenHelpCenter() =>
        NavigationManager.NavigateTo("/help", true);

    private void OpenNewPanel()
    {
        editingArticle = null;
        draftTitle = string.Empty;
        draftCategory = "Orders";
        draftStatus = "Draft";
        draftContent = string.Empty;
        panelOpen = true;
    }

    private void OpenEditPanel(HelpArticleModel article)
    {
        editingArticle = article;
        draftTitle = article.Title;
        draftCategory = article.Category;
        draftStatus = article.Status;
        draftContent = article.Content;
        panelOpen = true;
    }

    private void ClosePanel() => panelOpen = false;

    private void SaveArticle(bool publish)
    {
        if (string.IsNullOrWhiteSpace(draftTitle)) return;

        var status = publish ? "Published" : (string.IsNullOrWhiteSpace(draftStatus) ? "Draft" : draftStatus);
        if (!publish && status == "Published")
            status = "Draft";

        var updated = new HelpArticleModel
        {
            Title = draftTitle.Trim(),
            Category = draftCategory,
            Status = publish ? "Published" : status,
            Content = draftContent.Trim(),
            LastUpdated = DateTime.Now.ToString("MMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture)
        };

        if (editingArticle == null)
        {
            updated.Id = articles.Count == 0 ? 1 : articles.Max(a => a.Id) + 1;
            articles.Insert(0, updated);
            ShowToast("Article created");
        }
        else
        {
            updated.Id = editingArticle.Id;
            updated.Views = editingArticle.Views;
            updated.Helpful = editingArticle.Helpful;
            var idx = articles.FindIndex(a => a.Id == editingArticle.Id);
            if (idx >= 0) articles[idx] = updated;
            ShowToast("Article updated");
        }

        panelOpen = false;
    }

    private void TogglePublish(HelpArticleModel article)
    {
        article.Status = article.Status == "Published" ? "Draft" : "Published";
        article.LastUpdated = DateTime.Now.ToString("MMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture);
        ShowToast("Article status updated");
    }

    private void ConfirmDelete()
    {
        if (deleteTarget == null) return;
        articles.RemoveAll(a => a.Id == deleteTarget.Id);
        deleteTarget = null;
        ShowToast("Article deleted");
    }

    private void ShowToast(string message)
    {
        toast = message;
        toastTimer?.Dispose();
        toastTimer = new System.Threading.Timer(_ =>
        {
            _ = InvokeAsync(() =>
            {
                toast = null;
                StateHasChanged();
            });
        }, null, 2400, Timeout.Infinite);
    }

    public static string GetCategoryIconSvg(string key, string color) => key switch
    {
        "truck" => $"""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="{color}" stroke-width="2"><path d="M14 18V6a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2v11a1 1 0 0 0 1 1h2"/><path d="M15 18H9"/><path d="M19 18h2a1 1 0 0 0 1-1v-3.65a1 1 0 0 0-.22-.624l-3.48-4.35A1 1 0 0 0 17.52 8H14"/><circle cx="17" cy="18" r="2"/><circle cx="7" cy="18" r="2"/></svg>""",
        "return" => $"""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="{color}" stroke-width="2"><path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8"/><path d="M3 3v5h5"/></svg>""",
        "card" => $"""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="{color}" stroke-width="2"><rect width="20" height="14" x="2" y="5" rx="2"/><line x1="2" x2="22" y1="10" y2="10"/></svg>""",
        "settings" => $"""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="{color}" stroke-width="2"><path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z"/><circle cx="12" cy="12" r="3"/></svg>""",
        "help" => $"""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="{color}" stroke-width="2"><circle cx="12" cy="12" r="10"/><path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"/><path d="M12 17h.01"/></svg>""",
        _ => $"""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="{color}" stroke-width="2"><path d="M11 21.73a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73z"/><path d="M12 22V12"/><polyline points="3.29 7 12 12 20.71 7"/><path d="m7.5 4.27 9 5.15"/></svg>"""
    };

    public void Dispose() => toastTimer?.Dispose();
}
