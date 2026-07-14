using MuuqWear.Application.Content;
using MuuqWear.Application.Services.ContentService;
using MuuqWear.Application.Services.VoteService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.ContentItem;

namespace MuuqWear.Web.Services;

/// <summary>
/// Cached content tab counts and health metrics — one parallel fetch instead of many sequential calls.
/// </summary>
public sealed class AdminContentCountsCacheService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    private readonly IContentService _contentService;
    private readonly IVoteService _voteService;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private ContentCountsSnapshot? _snapshot;
    private DateTime _cachedAt;

    public AdminContentCountsCacheService(
        IContentService contentService,
        IVoteService voteService)
    {
        _contentService = contentService;
        _voteService = voteService;
    }

    public void Invalidate() => _snapshot = null;

    public async Task<ContentCountsSnapshot> GetSnapshotAsync(bool forceRefresh = false)
    {
        if (!forceRefresh
            && _snapshot != null
            && DateTime.UtcNow - _cachedAt < CacheDuration)
        {
            return _snapshot;
        }

        await _gate.WaitAsync();
        try
        {
            if (!forceRefresh
                && _snapshot != null
                && DateTime.UtcNow - _cachedAt < CacheDuration)
            {
                return _snapshot;
            }

            var journalTask = _contentService.GetAll(ContentCategory.JournalArticles);
            var designTask = _contentService.GetAll(ContentCategory.DesignHistory);
            var eventsTask = _contentService.GetAll(ContentCategory.Events);
            var voteActiveTask = _voteService.GetActiveItems();
            var voteFinishedTask = _voteService.GetFinishedItems();

            await Task.WhenAll(journalTask, designTask, eventsTask, voteActiveTask, voteFinishedTask);

            var journal = journalTask.Result;
            var design = designTask.Result;
            var events = eventsTask.Result;
            var voteActive = voteActiveTask.Result;
            var voteFinished = voteFinishedTask.Result;

            var journalItems = journal.Success && journal.Data != null ? journal.Data : [];
            var designItems = design.Success && design.Data != null ? design.Data : [];
            var eventItems = events.Success && events.Data != null ? events.Data : [];

            var voteIds = new HashSet<Guid>();
            if (voteActive.Success && voteActive.Data != null)
            {
                foreach (var item in voteActive.Data)
                    voteIds.Add(item.Id);
            }

            if (voteFinished.Success && voteFinished.Data != null)
            {
                foreach (var item in voteFinished.Data)
                    voteIds.Add(item.Id);
            }

            var snapshot = new ContentCountsSnapshot
            {
                TabCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["journal"] = journalItems.Count,
                    ["design-history"] = designItems.Count,
                    ["events"] = eventItems.Count,
                    ["vote"] = voteIds.Count,
                    ["media"] = CountMediaUrls(journalItems, eventItems, designItems)
                },
                HealthJournalLowSeoCount = journalItems.Count(x => JournalSeoScorer.Score(x) < 70),
                HealthJournalDraftCount = journalItems.Count(x =>
                    ContentItemStatusHelper.NormalizeStatus(x.Status) == "draft"),
                HealthVoteActiveCount = voteActive.Success && voteActive.Data != null
                    ? voteActive.Data.Count
                    : 0
            };

            _snapshot = snapshot;
            _cachedAt = DateTime.UtcNow;
            return snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static int CountMediaUrls(
        IReadOnlyList<ContentItemModel> journal,
        IReadOnlyList<ContentItemModel> events,
        IReadOnlyList<ContentItemModel> design)
    {
        var usage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        void RegisterAll(IEnumerable<ContentItemModel> items)
        {
            foreach (var item in items)
                RegisterMediaUsage(usage, item.ImageUrl, item.SecondImageUrl, item.Content);
        }

        RegisterAll(journal);
        RegisterAll(events);
        RegisterAll(design);

        return usage.Keys.Count(IsAllowedMediaUrl);
    }

    private static void RegisterMediaUsage(
        Dictionary<string, int> usage,
        params string?[] urlsAndJson)
    {
        foreach (var value in urlsAndJson)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (value.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/", StringComparison.Ordinal))
            {
                usage[value] = usage.GetValueOrDefault(value) + 1;
                continue;
            }

            foreach (var url in ExtractUrlsFromText(value))
                usage[url] = usage.GetValueOrDefault(url) + 1;
        }
    }

    private static IEnumerable<string> ExtractUrlsFromText(string text)
    {
        const string prefix = "http";
        var index = 0;
        while ((index = text.IndexOf(prefix, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var end = index;
            while (end < text.Length && !char.IsWhiteSpace(text[end]) && text[end] != '"' && text[end] != '\'')
                end++;

            yield return text[index..end];
            index = end;
        }
    }

    private static bool IsAllowedMediaUrl(string url)
    {
        if (url.StartsWith("/", StringComparison.Ordinal))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    public sealed class ContentCountsSnapshot
    {
        public Dictionary<string, int> TabCounts { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public int HealthJournalLowSeoCount { get; init; }
        public int HealthJournalDraftCount { get; init; }
        public int HealthVoteActiveCount { get; init; }
    }
}
