using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MuuqWear.Application.Services.HelpCenterService;
using MuuqWear.Model.HelpCenter;
using MuuqWear.Web.Services;

namespace MuuqWear.Web.Components.Pages.AdminComponent.Support;

public partial class AdminSupportKnowledgeBaseTab : IDisposable
{
    private const int PageSize = 10;

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IHelpCenterService HelpCenterService { get; set; } = default!;

    private List<HelpArticleModel> articles = [];
    private string search = string.Empty;
    private string catFilter = "All";
    private string statusFilter = "All";
    private int page = 1;
    private bool panelOpen;
    private bool isLoading = true;
    private bool isSaving;
    private bool isLoadingArticle;
    private bool searchExpanded;
    private ElementReference searchInputRef;
    private string? loadError;
    private string? listWarning;
    private HelpArticleModel? editingArticle;
    private HelpArticleModel? viewingArticle;
    private HelpArticleModel? deleteTarget;
    private string? toast;
    private System.Threading.Timer? toastTimer;

    private string draftTitle = string.Empty;
    private string draftCategory = "Orders";
    private string draftStatus = "Draft";
    private string draftContent = string.Empty;
    private string draftHeroImageUrl = string.Empty;
    private List<HelpArticleStepModel> draftSteps = [];

    private static readonly string[] StatusPills = ["All", "Published", "Draft"];

    protected override async Task OnInitializedAsync() => await LoadArticles();

    private async Task ToggleSearchAsync()
    {
        if (searchExpanded)
        {
            searchExpanded = false;
            return;
        }

        searchExpanded = true;
        await Task.Yield();
        try
        {
            await searchInputRef.FocusAsync();
        }
        catch (InvalidOperationException)
        {
            // Input not rendered yet.
        }
    }

    private async Task HandleSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape" && string.IsNullOrWhiteSpace(search))
        {
            searchExpanded = false;
            await Task.CompletedTask;
        }
    }

    private void ClearSearch()
    {
        search = string.Empty;
        page = 1;
        ResetPageIfNeeded();
    }

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

    private int TotalPages =>
        Math.Max(1, (int)Math.Ceiling(FilteredArticles.Count() / (double)PageSize));

    private int CurrentPage =>
        Math.Min(page, TotalPages);

    private IEnumerable<HelpArticleModel> PagedArticles =>
        FilteredArticles
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

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

    private async Task LoadArticles()
    {
        isLoading = true;
        loadError = null;

        try
        {
            var (items, error, truncation) = await SupportPaginatedLoader.LoadAllPagesAsync(
                (pageNum, pageSize) => HelpCenterService.GetAdminArticles(null, null, null, pageNum, pageSize));

            if (error != null && items.Count == 0)
            {
                loadError = AdminUiErrorHelper.FromApi(error, "Failed to load knowledge base articles.");
                listWarning = null;
                articles = [];
                return;
            }

            listWarning = truncation;
            articles = items
                .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
                .ToList();
            page = 1;
        }
        catch (Exception ex)
        {
            loadError = AdminUiErrorHelper.FromException(ex);
            articles = [];
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ResetPageIfNeeded()
    {
        if (page > TotalPages)
            page = TotalPages;
    }

    private void OnSearchChanged(ChangeEventArgs e)
    {
        search = e.Value?.ToString() ?? string.Empty;
        page = 1;
        ResetPageIfNeeded();

        if (!string.IsNullOrWhiteSpace(search))
            searchExpanded = true;
    }

    private void SetCatFilter(string cat)
    {
        catFilter = cat;
        page = 1;
        ResetPageIfNeeded();
    }

    private void SetStatusFilter(string status)
    {
        statusFilter = status;
        page = 1;
        ResetPageIfNeeded();
    }

    private void ToggleCatFilter(string cat) =>
        SetCatFilter(catFilter == cat ? "All" : cat);

    private void GoToPage(int nextPage)
    {
        page = Math.Clamp(nextPage, 1, TotalPages);
    }

    private void OpenHelpCenter() =>
        NavigationManager.NavigateTo("/help", true);

    private void ResetDraftFields()
    {
        draftTitle = string.Empty;
        draftCategory = "Orders";
        draftStatus = "Draft";
        draftContent = string.Empty;
        draftHeroImageUrl = string.Empty;
        draftSteps = [];
    }

    private void OpenNewPanel()
    {
        editingArticle = null;
        ResetDraftFields();
        panelOpen = true;
    }

    private async Task OpenEditPanelAsync(HelpArticleModel article)
    {
        isLoadingArticle = true;
        panelOpen = true;
        editingArticle = article;

        try
        {
            var result = await HelpCenterService.GetAdminArticleById(article.Id);
            if (!result.Success || result.Data == null)
            {
                ShowToast(AdminUiErrorHelper.FromApi(result.Message, "Failed to load article."));
                panelOpen = false;
                return;
            }

            ApplyArticleToDraft(result.Data);
        }
        catch (Exception ex)
        {
            ShowToast(AdminUiErrorHelper.FromException(ex));
            panelOpen = false;
        }
        finally
        {
            isLoadingArticle = false;
        }
    }

    private async Task OpenViewPanelAsync(HelpArticleModel article)
    {
        isLoadingArticle = true;
        viewingArticle = article;

        try
        {
            var result = await HelpCenterService.GetAdminArticleById(article.Id);
            if (!result.Success || result.Data == null)
            {
                ShowToast(AdminUiErrorHelper.FromApi(result.Message, "Failed to load article."));
                viewingArticle = null;
                return;
            }

            viewingArticle = result.Data;
        }
        catch (Exception ex)
        {
            ShowToast(AdminUiErrorHelper.FromException(ex));
            viewingArticle = null;
        }
        finally
        {
            isLoadingArticle = false;
        }
    }

    private void CloseViewPanel() => viewingArticle = null;

    private async Task EditFromViewAsync()
    {
        if (viewingArticle == null)
            return;

        var article = viewingArticle;
        viewingArticle = null;
        await OpenEditPanelAsync(article);
    }

    private void ApplyArticleToDraft(HelpArticleModel article)
    {
        editingArticle = article;
        draftTitle = article.Title;
        draftCategory = article.Category;
        draftStatus = article.Status;
        draftContent = article.Content;
        draftHeroImageUrl = article.HeroImageUrl ?? string.Empty;
        draftSteps = article.Steps
            .OrderBy(s => s.SortOrder)
            .Select(s => new HelpArticleStepModel
            {
                Id = s.Id,
                SortOrder = s.SortOrder,
                Detail = s.Detail,
                ImageUrl = s.ImageUrl
            })
            .ToList();
    }

    private void ClosePanel() => panelOpen = false;

    private void AddDraftStep()
    {
        draftSteps.Add(new HelpArticleStepModel
        {
            SortOrder = draftSteps.Count,
            Detail = string.Empty
        });
    }

    private void RemoveDraftStep(int index)
    {
        if (index < 0 || index >= draftSteps.Count)
            return;

        draftSteps.RemoveAt(index);
        for (var i = 0; i < draftSteps.Count; i++)
            draftSteps[i].SortOrder = i;
    }

    private Task SetDraftHeroImageUrl(string url)
    {
        draftHeroImageUrl = url;
        return Task.CompletedTask;
    }

    private Task SetDraftStepImageUrl(int index, string url)
    {
        if (index >= 0 && index < draftSteps.Count)
            draftSteps[index].ImageUrl = string.IsNullOrWhiteSpace(url) ? null : url;

        return Task.CompletedTask;
    }

    private SaveHelpArticleModel BuildSavePayload(bool publish)
    {
        var status = publish
            ? HelpArticleDisplayStatus.Published
            : (string.IsNullOrWhiteSpace(draftStatus) ? HelpArticleDisplayStatus.Draft : draftStatus);
        if (!publish && status == HelpArticleDisplayStatus.Published)
            status = HelpArticleDisplayStatus.Draft;

        var steps = draftSteps
            .Select((step, index) => new HelpArticleStepModel
            {
                Id = step.Id,
                SortOrder = index,
                Detail = step.Detail.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(step.ImageUrl) ? null : step.ImageUrl.Trim()
            })
            .Where(step => !string.IsNullOrWhiteSpace(step.Detail))
            .ToList();

        return new SaveHelpArticleModel
        {
            Title = draftTitle.Trim(),
            Category = draftCategory,
            Status = publish ? HelpArticleDisplayStatus.Published : status,
            Content = draftContent.Trim(),
            HeroImageUrl = string.IsNullOrWhiteSpace(draftHeroImageUrl) ? null : draftHeroImageUrl.Trim(),
            Steps = steps
        };
    }

    private async Task SaveArticleAsync(bool publish)
    {
        if (string.IsNullOrWhiteSpace(draftTitle) || isSaving)
            return;

        var payload = BuildSavePayload(publish);

        isSaving = true;
        try
        {
            var result = editingArticle == null
                ? await HelpCenterService.CreateArticle(payload)
                : await HelpCenterService.UpdateArticle(editingArticle.Id, payload);

            if (!result.Success || result.Data == null)
            {
                ShowToast(AdminUiErrorHelper.FromApi(result.Message, "Failed to save article."));
                return;
            }

            if (editingArticle == null)
                articles.Insert(0, result.Data);
            else
            {
                var idx = articles.FindIndex(a => a.Id == editingArticle.Id);
                if (idx >= 0)
                    articles[idx] = result.Data;
            }

            panelOpen = false;
            page = 1;
            ShowToast(editingArticle == null ? "Article created" : "Article updated");
        }
        catch (Exception ex)
        {
            ShowToast(AdminUiErrorHelper.FromException(ex));
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task TogglePublishAsync(HelpArticleModel article)
    {
        var next = article.Status == HelpArticleDisplayStatus.Published
            ? HelpArticleDisplayStatus.Draft
            : HelpArticleDisplayStatus.Published;

        try
        {
            var result = await HelpCenterService.UpdateArticleStatus(article.Id, next);
            if (!result.Success || result.Data == null)
            {
                ShowToast(AdminUiErrorHelper.FromApi(result.Message, "Failed to update article status."));
                return;
            }

            var idx = articles.FindIndex(a => a.Id == article.Id);
            if (idx >= 0)
                articles[idx] = result.Data;

            ShowToast("Article status updated");
        }
        catch (Exception ex)
        {
            ShowToast(AdminUiErrorHelper.FromException(ex));
        }
    }

    private async Task ConfirmDeleteAsync()
    {
        if (deleteTarget == null)
            return;

        try
        {
            var result = await HelpCenterService.DeleteArticle(deleteTarget.Id);
            if (!result.Success)
            {
                ShowToast(AdminUiErrorHelper.FromApi(result.Message, "Failed to delete article."));
                return;
            }

            articles.RemoveAll(a => a.Id == deleteTarget.Id);
            deleteTarget = null;
            ResetPageIfNeeded();
            ShowToast("Article deleted");
        }
        catch (Exception ex)
        {
            ShowToast(AdminUiErrorHelper.FromException(ex));
        }
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
