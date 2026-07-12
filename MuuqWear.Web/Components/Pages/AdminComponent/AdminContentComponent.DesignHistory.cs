using MuuqWear.Application.Content;
using MuuqWear.Application.Shared;
using MuuqWear.Model.ContentItem;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminContentComponent
{
    private IEnumerable<ContentItemModel> FilteredDesignItems =>
        items.Where(item =>
        {
            var q = searchQuery.Trim();
            if (string.IsNullOrEmpty(q))
                return true;

            return item.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                   || (item.Designer?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                   || (item.Collection?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                   || (item.Inspiration?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false);
        });

    private IEnumerable<(string Collection, IReadOnlyList<ContentItemModel> Items)> DesignHistoryGroups
    {
        get
        {
            var filtered = FilteredDesignItems.ToList();
            var collections = filtered
                .Select(x => string.IsNullOrWhiteSpace(x.Collection) ? "Uncategorized" : x.Collection!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var collection in collections)
            {
                var groupItems = filtered
                    .Where(x =>
                    {
                        var key = string.IsNullOrWhiteSpace(x.Collection) ? "Uncategorized" : x.Collection!;
                        return string.Equals(key, collection, StringComparison.OrdinalIgnoreCase);
                    })
                    .OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (groupItems.Count > 0)
                    yield return (collection, groupItems);
            }
        }
    }

    private int DesignPublishedCount =>
        ContentItemStatusHelper.CountByStatus(items, "published");

    private int DesignDraftCount =>
        items.Count(x => ContentItemStatusHelper.NormalizeStatus(x.Status) == "draft");

    private int DesignTotalViews => items.Sum(x => x.Views);

    private int DesignCollectionCount =>
        items.Select(x => string.IsNullOrWhiteSpace(x.Collection) ? "Uncategorized" : x.Collection!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

    private static string GetDesignStatusLabel(ContentItemModel item) =>
        ContentItemStatusHelper.IsArchivedStatus(item.Status)
            ? "Archived"
            : ContentItemStatusHelper.GetDisplayLabel(item);

    private static string GetDesignStatusBadgeClass(ContentItemModel item) =>
        ContentItemStatusHelper.GetStatusBadgeClass(item);

    private static string GetDesignMetaLine(ContentItemModel item)
    {
        var designer = string.IsNullOrWhiteSpace(item.Designer) ? "Unknown designer" : item.Designer;
        var year = string.IsNullOrWhiteSpace(item.Year) ? "—" : item.Year;
        var inspiration = string.IsNullOrWhiteSpace(item.Inspiration) ? "—" : item.Inspiration;
        return $"{designer} · {year} · {inspiration}";
    }

    private async Task HandleDesignSaveDraftAsync()
    {
        var saved = isEditMode
            ? await HandleEdit()
            : await HandleCreate();

        if (saved)
        {
            await NotifyContentMutatedAsync();
            ShowToast(isEditMode ? "Design entry updated" : "Design entry created");
        }
    }

    private async Task HandleDesignPublishAsync()
    {
        if (string.IsNullOrWhiteSpace(form.Title))
        {
            formError = "Design name is required";
            return;
        }

        var title = form.Title;
        Guid? targetId = null;

        if (isEditMode && editingItem != null)
        {
            targetId = editingItem.Id;
            var wasPublished = ContentItemStatusHelper.IsPublishedStatus(editingItem.Status);

            if (!await HandleEdit())
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

        if (!await HandleCreate())
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

    private async Task ToggleDesignPublishAsync(ContentItemModel item)
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
            ? $"\"{item.Title}\" unpublished"
            : $"\"{item.Title}\" published");
    }
}
