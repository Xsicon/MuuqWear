using MuuqWear.Application.Content;
using MuuqWear.Application.Shared;
using MuuqWear.Model.ContentItem;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminContentComponent
{
    private string journalCategoryFilter = "All";
    private string journalStatusFilter = "All";
    private string? toastMessage;
    private int toastGeneration;

    private readonly Dictionary<string, int> tabCounts = new()
    {
        ["journal"] = 0,
        ["events"] = 0,
        ["design-history"] = 0
    };

    private static readonly string[] JournalCategories =
        ["All", "Culture", "Design", "Innovation", "Lifestyle", "Tech"];

    private static readonly string[] JournalStatuses =
        ["All", "Published", "Draft"];

    private IEnumerable<ContentItemModel> FilteredJournalItems =>
        items.Where(item =>
        {
            var q = searchQuery.Trim();
            if (!string.IsNullOrEmpty(q))
            {
                var matchesSearch =
                    item.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (item.Author?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (item.Excerpt?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (item.Category?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false);

                if (!matchesSearch)
                    return false;
            }

            if (journalCategoryFilter != "All"
                && !string.Equals(item.Category, journalCategoryFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return journalStatusFilter switch
            {
                "Published" => item.IsPublished,
                "Draft" => !item.IsPublished,
                _ => true
            };
        });

    private int JournalPublishedCount => items.Count(x => x.IsPublished);
    private int JournalDraftCount => items.Count(x => !x.IsPublished);
    private int JournalTotalViews => items.Sum(x => x.Views);
    private int JournalLowSeoCount => items.Count(x => JournalSeoScorer.Score(x) < 70);

    private JournalSeoInput JournalFormSeoInput => new(
        form.Title,
        journalSeoTitle,
        form.Content,
        form.Category,
        form.ImageUrl,
        journalExcerpt);

    private int JournalFormSeoScore => JournalSeoScorer.Score(JournalFormSeoInput);

    private string JournalFormSeoSuggestion => JournalSeoScorer.GetTopSuggestion(JournalFormSeoInput);

    private void SetJournalCategoryFilter(string category) => journalCategoryFilter = category;

    private void SetJournalStatusFilter(string status) => journalStatusFilter = status;

    private static int EstimateReadTime(ContentItemModel item) =>
        item.ReadTimeMinutes ?? JournalSeoScorer.EstimateReadTimeMinutes(item.Content);

    private static string GetSeoBarColor(int score) => JournalSeoScorer.GetBarColor(score);

    private static string GetCategoryBadgeClass(string? category) =>
        category?.ToLowerInvariant() switch
        {
            "culture" => "content-cat--culture",
            "design" => "content-cat--design",
            "innovation" => "content-cat--innovation",
            "lifestyle" => "content-cat--lifestyle",
            "tech" => "content-cat--tech",
            _ => "content-cat--default"
        };

    private static string GetCategoryLabel(string? category) =>
        string.IsNullOrWhiteSpace(category) ? "Uncategorized" : category;

    private string GetCreateButtonLabel() => activeView switch
    {
        "events" => "New Event",
        "design-history" => "New Design Entry",
        _ => "New Article"
    };

    private void ShowToast(string message)
    {
        toastMessage = message;
        var generation = ++toastGeneration;
        _ = DismissToastAsync(generation);
    }

    private async Task DismissToastAsync(int generation)
    {
        await Task.Delay(2400);
        if (toastGeneration != generation)
            return;

        toastMessage = null;
        await InvokeAsync(StateHasChanged);
    }

    private string journalSeoTitle = string.Empty;
    private string journalExcerpt = string.Empty;
    private bool journalExcerptManuallyEdited;
    private bool journalIsFeatured;

    private int GetTabCount(string view) =>
        tabCounts.TryGetValue(view, out var count) ? count : 0;

    private async Task RefreshTabCountsAsync()
    {
        foreach (var tab in TabViews)
        {
            var result = await ContentService.GetAll(
                AdminContentTabCoordinator.ViewToCategory(tab.View));

            tabCounts[tab.View] = result.Success && result.Data != null
                ? result.Data.Count
                : 0;
        }
    }

    private void SyncActiveTabCount() => tabCounts[activeView] = items.Count;

    private string GetJournalAuthorLine(ContentItemModel item)
    {
        var author = string.IsNullOrWhiteSpace(item.Author) ? "Muuqwear Editorial" : item.Author;
        return $"{author} · {FormatItemDate(item)} · {EstimateReadTime(item)} min read";
    }

    private static string GetJournalThumbnail(ContentItemModel item) =>
        string.IsNullOrWhiteSpace(item.ImageUrl)
            ? "https://images.unsplash.com/photo-1508427953056-b00b8d78ebf5?w=120&h=80&fit=crop"
            : item.ImageUrl;

    private static string GetJournalPublicUrl(ContentItemModel item)
    {
        if (!item.IsPublished)
            return "/journal";

        if (!string.IsNullOrWhiteSpace(item.Slug))
            return $"/journal?article={Uri.EscapeDataString(item.Slug)}";

        return $"/journal?article={item.Id}";
    }

    private void InitJournalFormFields()
    {
        form.Author = "Muuqwear Editorial";
        journalSeoTitle = " | Muuqwear Journal";
        journalExcerpt = string.Empty;
        journalExcerptManuallyEdited = false;
        journalIsFeatured = false;
    }

    private void LoadJournalPanelFromItem(ContentItemModel item)
    {
        journalSeoTitle = !string.IsNullOrWhiteSpace(item.SeoTitle)
            ? item.SeoTitle
            : string.IsNullOrWhiteSpace(item.Title)
                ? " | Muuqwear Journal"
                : $"{item.Title} | Muuqwear Journal";

        journalExcerpt = !string.IsNullOrWhiteSpace(item.Excerpt)
            ? item.Excerpt
            : ExtractExcerptFromContent(item.Content);

        journalExcerptManuallyEdited = !string.IsNullOrWhiteSpace(item.Excerpt);
        journalIsFeatured = item.IsFeatured;
    }

    private void ApplyJournalPanelToForm()
    {
        if (ActiveCategory != ContentCategory.JournalArticles)
            return;

        form.Author = string.IsNullOrWhiteSpace(form.Author) ? "Muuqwear Editorial" : form.Author;
        form.Excerpt = journalExcerpt;
        form.SeoTitle = journalSeoTitle;
        form.IsFeatured = journalIsFeatured;
        form.Slug = JournalSeoScorer.BuildSlug(form.Title);
        form.ReadTimeMinutes = JournalSeoScorer.EstimateReadTimeMinutes(form.Content);
    }

    private UpdateContentItemModel BuildUpdateModel() =>
        ActiveCategory == ContentCategory.JournalArticles
            ? new UpdateContentItemModel
            {
                Title = form.Title,
                Content = form.Content,
                Category = form.Category,
                ImageUrl = form.ImageUrl,
                Author = form.Author,
                Excerpt = form.Excerpt,
                Slug = form.Slug,
                SeoTitle = form.SeoTitle,
                Tags = form.Tags,
                IsFeatured = form.IsFeatured,
                ScheduledAt = form.ScheduledAt,
                ReadTimeMinutes = form.ReadTimeMinutes
            }
            : new UpdateContentItemModel
            {
                Title = form.Title,
                Content = form.Content,
                Category = form.Category,
                ImageUrl = form.ImageUrl,
                Designer = form.Designer,
                Year = form.Year,
                Inspiration = form.Inspiration,
                Collection = form.Collection,
                SecondImageUrl = form.SecondImageUrl,
                TechnicalFabric = form.TechnicalFabric,
                TechnicalTechniques = form.TechnicalTechniques,
                TechnicalProduction = form.TechnicalProduction,
                TechnicalAvailability = form.TechnicalAvailability,
                ProductId = form.ProductId
            };

    private void OnJournalFormFieldChanged()
    {
        if (!journalExcerptManuallyEdited)
            journalExcerpt = ExtractExcerptFromContent(form.Content);
    }

    private void OnJournalExcerptChanged() => journalExcerptManuallyEdited = true;

    private static string ExtractExcerptFromContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var normalized = content.Trim().ReplaceLineEndings(" ");
        return normalized.Length > 160 ? normalized[..160] : normalized;
    }

    private void OnJournalTitleChanged()
    {
        journalSeoTitle = string.IsNullOrWhiteSpace(form.Title)
            ? " | Muuqwear Journal"
            : $"{form.Title} | Muuqwear Journal";
        OnJournalFormFieldChanged();
    }

    private string GetJournalSlugPreview()
    {
        var slug = !string.IsNullOrWhiteSpace(form.Slug)
            ? form.Slug
            : JournalSeoScorer.BuildSlug(form.Title);

        return string.IsNullOrWhiteSpace(slug)
            ? "https://muuqwear.com/journal/"
            : $"https://muuqwear.com/journal/{slug}";
    }

    private async Task HandleJournalSaveDraftAsync()
    {
        var saved = isEditMode
            ? await HandleEdit()
            : await HandleCreate();

        if (saved)
            ShowToast(isEditMode ? "Article updated" : "Article saved");
    }

    private async Task HandleJournalPublishAsync()
    {
        if (string.IsNullOrWhiteSpace(form.Title))
        {
            formError = "Title is required";
            return;
        }

        var title = form.Title;

        if (isEditMode && editingItem != null)
        {
            var id = editingItem.Id;
            var wasPublished = editingItem.IsPublished;

            if (!await HandleEdit())
                return;

            if (!wasPublished && !await HandlePublish(id))
            {
                ShowToast($"Saved draft, but publish failed for \"{title}\".");
                return;
            }

            ShowToast(wasPublished
                ? $"\"{title}\" updated"
                : $"\"{title}\" published");
            return;
        }

        if (!await HandleCreate())
            return;

        var created = items.FirstOrDefault();
        if (created == null)
            return;

        if (!created.IsPublished && !await HandlePublish(created.Id))
        {
            ShowToast($"Saved draft, but publish failed for \"{title}\".");
            return;
        }

        ShowToast($"\"{title}\" published");
    }

    private async Task ToggleJournalPublishAsync(ContentItemModel item)
    {
        var wasPublished = item.IsPublished;
        var success = wasPublished
            ? await HandleUnpublish(item.Id)
            : await HandlePublish(item.Id);

        if (!success)
        {
            ShowToast(wasPublished
                ? $"Failed to unpublish \"{item.Title}\"."
                : $"Failed to publish \"{item.Title}\".");
            return;
        }

        ShowToast(wasPublished
            ? $"\"{item.Title}\" moved to draft"
            : $"\"{item.Title}\" published");
    }

    private static void ReplaceItemInList(List<ContentItemModel> list, ContentItemModel updated)
    {
        var index = list.FindIndex(x => x.Id == updated.Id);
        if (index >= 0)
            list[index] = updated;
    }
}
