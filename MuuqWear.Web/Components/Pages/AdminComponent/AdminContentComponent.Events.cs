using MuuqWear.Application.Content;
using MuuqWear.Application.Shared;
using MuuqWear.Model.ContentItem;
using MuuqWear.Model.Muuqsimo;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminContentComponent
{
    private readonly Dictionary<Guid, int> eventTicketSoldByProductId = new();

    private IEnumerable<ContentItemModel> FilteredEventItems =>
        items.Where(item =>
        {
            if (statusFilter == "published"
                && !ContentItemStatusHelper.IsPublishedStatus(item.Status))
                return false;

            if (statusFilter == "draft"
                && !ContentItemStatusHelper.IsDraftStatus(item.Status))
                return false;

            if (string.IsNullOrWhiteSpace(searchQuery))
                return true;

            var q = searchQuery.Trim();
            return item.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                   || GetEventLocation(item).Contains(q, StringComparison.OrdinalIgnoreCase)
                   || GetEventType(item).Contains(q, StringComparison.OrdinalIgnoreCase);
        });

    private int EventUpcomingCount => items.Count(x =>
        !ContentItemStatusHelper.IsArchivedStatus(x.Status)
        && EventDateHelper.IsUpcoming(ParseEventContent(x).StartDate, DateTime.UtcNow));

    private int EventPublishedCount =>
        ContentItemStatusHelper.CountByStatus(items, "published");

    private int EventDraftCount =>
        ContentItemStatusHelper.CountByStatus(items, "draft");

    private int EventTotalTicketsSold => items.Sum(x => GetEventTicketSalesInfo(x).Sold);

    private static MuuqsimoPageContentModel ParseEventContent(ContentItemModel item) =>
        EventPageContentSerializer.Parse(item.Content);

    private static string GetEventType(ContentItemModel item) =>
        string.IsNullOrWhiteSpace(item.Category) ? "Event" : item.Category!;

    private static string GetEventDateLabel(ContentItemModel item)
    {
        var date = ParseEventContent(item).StartDate;
        if (!string.IsNullOrWhiteSpace(date))
            return date!;

        var fallback = item.PublishedAt ?? item.CreatedAt;
        return fallback?.ToString("MMM dd, yyyy") ?? "—";
    }

    private static string GetEventLocation(ContentItemModel item)
    {
        var venue = ParseEventContent(item).Venue;
        if (venue == null)
            return "—";

        if (!string.IsNullOrWhiteSpace(venue.Name) && !string.IsNullOrWhiteSpace(venue.Address))
            return $"{venue.Name}, {venue.Address}";

        return venue.Name ?? venue.Address ?? "—";
    }

    private static (string Month, string Day) GetEventDateParts(ContentItemModel item) =>
        EventDateHelper.GetDateParts(GetEventDateLabel(item));

    private (int Sold, int Capacity) GetEventTicketSalesInfo(ContentItemModel item)
    {
        var content = ParseEventContent(item);
        var tiers = content.Tickets?.TierConfigs ?? new List<MuuqsimoTicketTierConfigModel>();
        var capacity = tiers.Sum(t => t.TotalCapacity);
        var sold = tiers.Sum(t =>
        {
            if (t.ProductId == Guid.Empty)
                return 0;

            if (eventTicketSoldByProductId.TryGetValue(t.ProductId, out var ticketSold))
                return ticketSold;

            return 0;
        });

        return (sold, capacity);
    }

    private bool EventHasTicketSalesData(ContentItemModel item)
    {
        var tiers = ParseEventContent(item).Tickets?.TierConfigs ?? new List<MuuqsimoTicketTierConfigModel>();
        return tiers.Any(t => t.ProductId != Guid.Empty && t.TotalCapacity > 0);
    }

    private async Task LoadEventTicketSalesAsync()
    {
        eventTicketSoldByProductId.Clear();

        var tiers = items
            .SelectMany(i => ParseEventContent(i).Tickets?.TierConfigs
                ?? new List<MuuqsimoTicketTierConfigModel>())
            .Where(t => t.ProductId != Guid.Empty && t.TotalCapacity > 0)
            .GroupBy(t => t.ProductId)
            .Select(g => g.First())
            .ToList();

        var loadTasks = tiers.Select(async tier =>
        {
            var result = await ProductService.GetById(tier.ProductId);
            if (!result.Success || result.Data == null)
                return (tier.ProductId, 0);

            var sold = Math.Max(0, tier.TotalCapacity - result.Data.Stock);
            return (tier.ProductId, sold);
        });

        foreach (var (productId, sold) in await Task.WhenAll(loadTasks))
            eventTicketSoldByProductId[productId] = sold;
    }

    private static string GetEventRsvpBarColor(int fillPercent) =>
        fillPercent >= 90 ? "#C44545" : fillPercent >= 60 ? "#F59E0B" : "#22C55E";

    private static string GetEventStatusLabel(ContentItemModel item) =>
        ContentItemStatusHelper.GetDisplayLabel(item);

    private static string GetEventStatusBadgeClass(ContentItemModel item) =>
        ContentItemStatusHelper.GetStatusBadgeClass(item);

    private async Task HandleEventSaveDraftAsync()
    {
        var saved = isEditMode
            ? await HandleEdit()
            : await HandleCreate();

        if (saved)
        {
            await NotifyContentMutatedAsync();
            ShowToast(isEditMode ? "Event updated" : "Event created");
        }
    }

    private async Task HandleEventPublishAsync()
    {
        if (string.IsNullOrWhiteSpace(form.Title))
        {
            formError = "Event name is required";
            return;
        }

        var title = form.Title;
        Guid? createdOrEditedId = null;

        if (isEditMode && editingItem != null)
        {
            createdOrEditedId = editingItem.Id;
            var wasPublished = ContentItemStatusHelper.IsPublishedStatus(editingItem.Status);

            if (!await HandleEdit())
                return;

            if (!wasPublished && createdOrEditedId.HasValue
                && !await HandlePublish(createdOrEditedId.Value))
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

        createdOrEditedId = items.FirstOrDefault()?.Id;
        if (createdOrEditedId == null)
            return;

        if (!ContentItemStatusHelper.IsPublishedStatus(items.First().Status)
            && !await HandlePublish(createdOrEditedId.Value))
        {
            ShowToast($"Saved draft, but publish failed for \"{title}\".");
            return;
        }

        await NotifyContentMutatedAsync();
        ShowToast($"\"{title}\" published");
    }

    private async Task ToggleEventPublishAsync(ContentItemModel item)
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
