using Microsoft.AspNetCore.Components;
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
    private string mediaError = string.Empty;
    private bool disposed;
    private string loadedView = string.Empty;
    private string journalFormStatus = "Draft";
    private string journalScheduledAtLocal = string.Empty;
    private readonly SemaphoreSlim viewSyncLock = new(1, 1);

    private readonly Dictionary<string, int> tabCounts = new()
    {
        ["journal"] = 0,
        ["events"] = 0,
        ["design-history"] = 0,
        ["vote"] = 0,
        ["media"] = 0
    };

    private static readonly string[] JournalCategories =
        ["All", "Culture", "Design", "Innovation", "Lifestyle", "Tech"];

    private static readonly string[] JournalStatuses =
        ["All", "Published", "Draft", "Scheduled"];

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
                "Published" => ContentItemStatusHelper.IsPublishedStatus(item.Status),
                "Draft" => ContentItemStatusHelper.NormalizeStatus(item.Status) == "draft",
                "Scheduled" => ContentItemStatusHelper.IsScheduledStatus(item.Status),
                _ => true
            };
        });

    private int JournalPublishedCount =>
        ContentItemStatusHelper.CountByStatus(items, "published");

    private int JournalDraftCount =>
        items.Count(x => ContentItemStatusHelper.NormalizeStatus(x.Status) == "draft");

    private int JournalScheduledCount =>
        ContentItemStatusHelper.CountByStatus(items, "scheduled");
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
        "vote" => "New Campaign",
        _ => "New Article"
    };

    private void ShowToast(string message)
    {
        if (disposed)
            return;

        toastMessage = message;
        var generation = ++toastGeneration;
        _ = DismissToastAsync(generation);
    }

    private async Task DismissToastAsync(int generation)
    {
        await Task.Delay(2400);
        if (disposed || toastGeneration != generation)
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

    private async Task RefreshTabCountsAsync(bool allTabs = true)
    {
        var tabs = allTabs
            ? TabViews.Select(t => t.View)
            : new[] { activeView };

        foreach (var tabView in tabs)
        {
            if (tabView == "vote")
            {
                if (allTabs || activeView == "vote")
                {
                    var active = await VoteService.GetActiveItems();
                    var finished = await VoteService.GetFinishedItems();

                    var ids = new HashSet<Guid>();
                    if (active.Success && active.Data != null)
                    {
                        foreach (var item in active.Data)
                            ids.Add(item.Id);
                    }

                    if (finished.Success && finished.Data != null)
                    {
                        foreach (var item in finished.Data)
                            ids.Add(item.Id);
                    }

                    tabCounts[tabView] = ids.Count;
                }

                continue;
            }

            if (tabView == "media")
            {
                tabCounts[tabView] = activeView == "media"
                    ? mediaItems.Count
                    : await CountMediaLibraryAsync();
                continue;
            }

            var result = await ContentService.GetAll(
                AdminContentTabCoordinator.ViewToCategory(tabView));

            tabCounts[tabView] = result.Success && result.Data != null
                ? result.Data.Count
                : 0;
        }
    }

    private async Task NotifyContentMutatedAsync()
    {
        if (activeView == "events")
            await LoadEventTicketSalesAsync();

        SyncActiveTabCount();
        await RefreshTabCountsAsync(allTabs: false);
        await RefreshHealthMetricsAsync();
    }

    private void SyncActiveTabCount() => tabCounts[activeView] = items.Count;

    private string GetJournalAuthorLine(ContentItemModel item)
    {
        var author = string.IsNullOrWhiteSpace(item.Author) ? "Muuqwear Editorial" : item.Author;
        return $"{author} · {FormatItemDate(item)} · {EstimateReadTime(item)} min read";
    }

    private static string GetJournalThumbnail(ContentItemModel item) =>
        string.IsNullOrWhiteSpace(item.ImageUrl)
            ? "/images/placeholder-content.svg"
            : item.ImageUrl;

    private static string GetJournalStatusLabel(ContentItemModel item) =>
        ContentItemStatusHelper.GetDisplayLabel(item);

    private static string GetJournalStatusBadgeClass(ContentItemModel item) =>
        ContentItemStatusHelper.GetStatusBadgeClass(item);

    private static string GetJournalPublicUrl(ContentItemModel item)
    {
        if (!ContentItemStatusHelper.CanPreviewOnSite(item))
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
        journalFormStatus = "Draft";
        journalScheduledAtLocal = string.Empty;
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
        journalFormStatus = ContentItemStatusHelper.GetDisplayLabel(item);
        journalScheduledAtLocal = item.ScheduledAt?.ToLocalTime().ToString("yyyy-MM-ddTHH:mm") ?? string.Empty;
    }

    private void ApplyJournalPanelToForm(bool preparingToPublish = false)
    {
        if (ActiveCategory != ContentCategory.JournalArticles)
            return;

        form.Author = string.IsNullOrWhiteSpace(form.Author) ? "Muuqwear Editorial" : form.Author;
        form.Excerpt = journalExcerpt;
        form.SeoTitle = journalSeoTitle;
        form.IsFeatured = journalIsFeatured;
        form.Slug = JournalSeoScorer.BuildSlug(form.Title);
        form.ReadTimeMinutes = JournalSeoScorer.EstimateReadTimeMinutes(form.Content);

        if (preparingToPublish)
        {
            form.Status = "draft";
            form.ScheduledAt = null;
            return;
        }

        form.Status = ContentItemStatusHelper.NormalizeStatus(journalFormStatus);
        if (ContentItemStatusHelper.IsScheduledStatus(form.Status))
            form.ScheduledAt = ParseJournalScheduledAtLocal();
        else
            form.ScheduledAt = null;
    }

    private void OnJournalScheduledAtChanged(ChangeEventArgs e) =>
        journalScheduledAtLocal = e.Value?.ToString() ?? string.Empty;

    private DateTime? ParseJournalScheduledAtLocal()
    {
        if (string.IsNullOrWhiteSpace(journalScheduledAtLocal))
            return null;

        return DateTime.TryParse(
            journalScheduledAtLocal,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeLocal,
            out var local)
            ? local.ToUniversalTime()
            : null;
    }

    private bool TryValidateJournalSchedule()
    {
        if (ActiveCategory != ContentCategory.JournalArticles)
            return true;

        if (!ContentItemStatusHelper.IsScheduledStatus(form.Status))
            return true;

        if (string.IsNullOrWhiteSpace(journalScheduledAtLocal))
        {
            formError = "Publish date is required for scheduled articles.";
            return false;
        }

        var scheduledAt = ParseJournalScheduledAtLocal();
        if (scheduledAt == null)
        {
            formError = "Publish date is invalid.";
            return false;
        }

        if (scheduledAt.Value <= DateTime.UtcNow)
        {
            formError = "Publish date must be in the future.";
            return false;
        }

        form.ScheduledAt = scheduledAt;
        return true;
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
                ReadTimeMinutes = form.ReadTimeMinutes,
                Status = form.Status
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

    private async Task<bool> ClearOtherFeaturedArticlesAsync(Guid? keepId = null)
    {
        foreach (var featured in items.Where(x => x.IsFeatured && x.Id != keepId).ToList())
        {
            var result = await ContentService.Update(
                ContentCategory.JournalArticles,
                featured.Id,
                BuildJournalFeaturedUpdate(featured, false));

            if (!result.Success || result.Data == null)
            {
                formError = result.Message ?? $"Failed to clear hero status from \"{featured.Title}\".";
                return false;
            }

            ReplaceItemInList(items, result.Data);
        }

        return true;
    }

    private async Task HandleJournalSaveDraftAsync()
    {
        ApplyJournalPanelToForm();
        if (!TryValidateJournalSchedule())
            return;

        if (ActiveCategory == ContentCategory.JournalArticles && journalIsFeatured)
        {
            if (!await ClearOtherFeaturedArticlesAsync(isEditMode ? editingItem?.Id : null))
            {
                ShowToast(formError);
                return;
            }
        }

        var saved = isEditMode
            ? await HandleEdit(skipJournalPanelApply: true)
            : await HandleCreate(skipJournalPanelApply: true);

        if (saved)
        {
            await NotifyContentMutatedAsync();
            var label = ContentItemStatusHelper.IsScheduledStatus(form.Status)
                ? "Article scheduled"
                : isEditMode ? "Article updated" : "Article saved";
            ShowToast(label);
        }
    }

    private async Task HandleJournalPublishAsync()
    {
        if (string.IsNullOrWhiteSpace(form.Title))
        {
            formError = "Title is required";
            return;
        }

        var title = form.Title;
        ApplyJournalPanelToForm(preparingToPublish: true);

        if (ActiveCategory == ContentCategory.JournalArticles && journalIsFeatured)
        {
            if (!await ClearOtherFeaturedArticlesAsync(isEditMode ? editingItem?.Id : null))
            {
                ShowToast(formError);
                return;
            }
        }

        Guid? targetId = null;

        if (isEditMode && editingItem != null)
        {
            targetId = editingItem.Id;
            var wasPublished = ContentItemStatusHelper.IsPublishedStatus(editingItem.Status);

            if (!await HandleEdit(skipJournalPanelApply: true))
                return;

            if (!wasPublished && targetId.HasValue && !await HandlePublish(targetId.Value))
            {
                ShowToast($"Saved draft, but publish failed for \"{title}\".");
                return;
            }

            await NotifyContentMutatedAsync();
            ShowToast(wasPublished
                ? $"\"{title}\" updated"
                : $"\"{title}\" published");
            return;
        }

        if (!await HandleCreate(skipJournalPanelApply: true))
            return;

        targetId = items.FirstOrDefault()?.Id;
        if (targetId == null)
            return;

        if (!ContentItemStatusHelper.IsPublishedStatus(items.First().Status)
            && !await HandlePublish(targetId.Value))
        {
            ShowToast($"Saved draft, but publish failed for \"{title}\".");
            return;
        }

        await NotifyContentMutatedAsync();
        ShowToast($"\"{title}\" published");
    }

    private async Task ToggleJournalPublishAsync(ContentItemModel item)
    {
        var wasPublished = ContentItemStatusHelper.IsPublishedStatus(item.Status);
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

        await NotifyContentMutatedAsync();
        ShowToast(wasPublished
            ? $"\"{item.Title}\" moved to draft"
            : $"\"{item.Title}\" published");
    }

    private async Task ToggleJournalFeaturedAsync(ContentItemModel item)
    {
        if (item.IsFeatured)
        {
            var update = BuildJournalFeaturedUpdate(item, false);
            var result = await ContentService.Update(ContentCategory.JournalArticles, item.Id, update);
            if (!result.Success || result.Data == null)
            {
                ShowToast(result.Message ?? "Failed to update featured status.");
                return;
            }

            ReplaceItemInList(items, result.Data);
            await NotifyContentMutatedAsync();
            ShowToast($"\"{item.Title}\" removed from hero");
            return;
        }

        foreach (var featured in items.Where(x => x.IsFeatured && x.Id != item.Id).ToList())
        {
            var clearResult = await ContentService.Update(
                ContentCategory.JournalArticles,
                featured.Id,
                BuildJournalFeaturedUpdate(featured, false));

            if (!clearResult.Success || clearResult.Data == null)
            {
                ShowToast(clearResult.Message ?? $"Failed to clear hero status from \"{featured.Title}\".");
                return;
            }

            ReplaceItemInList(items, clearResult.Data);
        }

        var setResult = await ContentService.Update(
            ContentCategory.JournalArticles,
            item.Id,
            BuildJournalFeaturedUpdate(item, true));

        if (!setResult.Success || setResult.Data == null)
        {
            ShowToast(setResult.Message ?? "Failed to set hero article.");
            return;
        }

        ReplaceItemInList(items, setResult.Data);
        await NotifyContentMutatedAsync();
        ShowToast("Hero article updated");
    }

    private static UpdateContentItemModel BuildJournalFeaturedUpdate(
        ContentItemModel item,
        bool isFeatured) =>
        new()
        {
            Title = item.Title,
            Content = item.Content,
            Category = item.Category,
            ImageUrl = item.ImageUrl,
            Author = item.Author,
            Excerpt = item.Excerpt,
            Slug = item.Slug,
            SeoTitle = item.SeoTitle,
            Tags = item.Tags,
            IsFeatured = isFeatured,
            ScheduledAt = item.ScheduledAt,
            ReadTimeMinutes = item.ReadTimeMinutes
        };

    private static void ReplaceItemInList(List<ContentItemModel> list, ContentItemModel updated)
    {
        var index = list.FindIndex(x => x.Id == updated.Id);
        if (index >= 0)
            list[index] = updated;
    }
}
