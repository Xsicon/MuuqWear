using MuuqWear.Application.Services.MuuqsimoService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.ContentItem;
using MuuqWear.Model.Muuqsimo;

namespace MuuqWear.Web.Services;

public sealed class FirestoreMuuqsimoService : IMuuqsimoService
{
    private const string DefaultSlug = "muuqsimo-2025";
    private readonly AdminFirestore _store;

    public FirestoreMuuqsimoService(AdminFirestore store) => _store = store;

    public async Task<Response<List<MuuqsimoEventSummaryModel>>> GetPublishedEventsAsync()
    {
        var events = await LoadPublishedEventsAsync();
        return Response<List<MuuqsimoEventSummaryModel>>.SuccessResponse(
            events.Select(ToSummary).ToList());
    }

    public async Task<Response<MuuqsimoPageModel>> GetPageAsync(string? slug = null)
    {
        var events = await LoadPublishedEventsAsync();
        if (events.Count == 0)
            return Response<MuuqsimoPageModel>.Fail("Unable to load Muuqsimo event page.");

        var wanted = string.IsNullOrWhiteSpace(slug) ? DefaultSlug : slug.Trim();
        var match = events.FirstOrDefault(e => SlugEquals(e.Content, wanted))
            ?? events.FirstOrDefault(e => SlugEquals(e.Content, DefaultSlug))
            ?? events[0];

        var page = new MuuqsimoPageModel
        {
            EventId = match.Item.Id,
            Title = match.Item.Title,
            Content = match.Content,
            TicketTiers = await ResolveTicketsAsync(match.Content)
        };
        return Response<MuuqsimoPageModel>.SuccessResponse(page);
    }

    private async Task<List<EventRow>> LoadPublishedEventsAsync()
    {
        var rows = (await _store.ListContentAsync(ContentCategory.Events))
            .Where(i => i.IsPublished)
            .Select(item =>
            {
                var content = EventPageContentSerializer.Parse(item.Content);
                if (string.IsNullOrWhiteSpace(content.Slug))
                    content.Slug = string.IsNullOrWhiteSpace(item.Slug) ? DefaultSlug : item.Slug;
                return new EventRow(item, content);
            })
            .OrderByDescending(e => e.Item.PublishedAt ?? e.Item.CreatedAt)
            .ToList();

        return rows;
    }

    private async Task<List<MuuqsimoTicketTierModel>> ResolveTicketsAsync(MuuqsimoPageContentModel content)
    {
        var tiers = new List<MuuqsimoTicketTierModel>();
        foreach (var cfg in content.Tickets?.TierConfigs ?? [])
        {
            if (cfg.ProductId == Guid.Empty)
                continue;

            var product = await _store.GetProductAsync(cfg.ProductId);
            var stock = product?.SizeStock.Sum(s => s.Quantity) ?? 0;
            tiers.Add(new MuuqsimoTicketTierModel
            {
                ProductId = cfg.ProductId,
                Name = product?.Name ?? "Ticket",
                Price = product?.Price ?? 0,
                ImageUrl = product?.ImageUrl,
                Stock = stock,
                Availability = stock <= 0 ? "Sold out" : $"{stock} remaining",
                Perks = cfg.Perks ?? [],
                IsHighlighted = cfg.IsHighlighted,
                Badge = cfg.Badge ?? product?.Badge
            });
        }

        return tiers;
    }

    private static MuuqsimoEventSummaryModel ToSummary(EventRow row) => new()
    {
        EventId = row.Item.Id,
        Title = row.Item.Title,
        Slug = row.Content.Slug ?? DefaultSlug,
        Tagline = row.Content.Tagline,
        StartDate = row.Content.StartDate
    };

    private static bool SlugEquals(MuuqsimoPageContentModel content, string slug) =>
        string.Equals(content.Slug, slug, StringComparison.OrdinalIgnoreCase);

    private sealed record EventRow(ContentItemModel Item, MuuqsimoPageContentModel Content);
}
