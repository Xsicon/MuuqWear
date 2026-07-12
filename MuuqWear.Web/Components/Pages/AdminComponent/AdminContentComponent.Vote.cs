using MuuqWear.Model.Vote;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminContentComponent
{
    private List<VoteItemModel> voteCampaigns = new();
    private VoteStatsModel? voteStats;
    private string voteStatusFilter = "All";
    private bool voteItemsLoading;

    private static readonly string[] VoteStatusFilters =
        ["All", "Active", "Pre-Order Ready", "In Production", "Completed", "Draft"];

    private IEnumerable<VoteItemModel> FilteredVoteCampaigns =>
        voteCampaigns.Where(c =>
            voteStatusFilter == "All"
            || string.Equals(MapVoteDisplayStatus(c), voteStatusFilter, StringComparison.OrdinalIgnoreCase));

    private int VoteActiveCount => voteCampaigns.Count(c =>
        string.Equals(MapVoteDisplayStatus(c), "Active", StringComparison.OrdinalIgnoreCase));

    private int VoteTotalVotes => voteCampaigns.Sum(c => c.VoteCount);

    private int VoteCompletedCount => voteCampaigns.Count(c =>
        string.Equals(MapVoteDisplayStatus(c), "Completed", StringComparison.OrdinalIgnoreCase));

    private int VotePreOrderPipelineCount => voteCampaigns.Count(c =>
    {
        var status = MapVoteDisplayStatus(c);
        return status is "Pre-Order Ready" or "In Production";
    });

    private void SetVoteStatusFilter(string filter) => voteStatusFilter = filter;

    private async Task LoadVoteItemsAsync()
    {
        voteItemsLoading = true;
        pageError = string.Empty;

        try
        {
            var activeTask = VoteService.GetActiveItems();
            var finishedTask = VoteService.GetFinishedItems();
            var statsTask = VoteService.GetStats();
            await Task.WhenAll(activeTask, finishedTask, statsTask);

            var active = activeTask.Result;
            var finished = finishedTask.Result;
            voteStats = statsTask.Result.Success ? statsTask.Result.Data : null;

            voteCampaigns = new List<VoteItemModel>();
            if (active.Success && active.Data != null)
                voteCampaigns.AddRange(active.Data);
            if (finished.Success && finished.Data != null)
                voteCampaigns.AddRange(finished.Data);

            voteCampaigns = voteCampaigns
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .OrderByDescending(x => x.CreatedAt ?? DateTime.MinValue)
                .ToList();

            if (!active.Success && !finished.Success)
                pageError = active.Message ?? finished.Message ?? "Failed to load vote campaigns.";

            tabCounts["vote"] = voteCampaigns.Count;
        }
        finally
        {
            voteItemsLoading = false;
        }
    }

    private static string MapVoteDisplayStatus(VoteItemModel item)
    {
        if (!string.IsNullOrWhiteSpace(item.Status))
        {
            return item.Status switch
            {
                var s when s.Contains("production", StringComparison.OrdinalIgnoreCase) => "In Production",
                var s when s.Contains("pre-order", StringComparison.OrdinalIgnoreCase) => "Pre-Order Ready",
                var s when s.Contains("complete", StringComparison.OrdinalIgnoreCase) => "Completed",
                var s when s.Contains("draft", StringComparison.OrdinalIgnoreCase) => "Draft",
                var s when s.Contains("active", StringComparison.OrdinalIgnoreCase) => "Active",
                _ => item.Status!
            };
        }

        if (!string.IsNullOrWhiteSpace(item.Tag))
        {
            if (item.Tag.Contains("PRODUCTION", StringComparison.OrdinalIgnoreCase))
                return "In Production";
            if (item.Tag.Contains("WINNER", StringComparison.OrdinalIgnoreCase)
                || item.Tag.Contains("PRE-ORDER", StringComparison.OrdinalIgnoreCase))
                return "Pre-Order Ready";
            if (item.Tag.Contains("SHIPPED", StringComparison.OrdinalIgnoreCase))
                return "Completed";
        }

        return "Active";
    }

    private static string GetVoteBadge(VoteItemModel item) =>
        string.IsNullOrWhiteSpace(item.Tag) ? MapVoteDisplayStatus(item).ToUpperInvariant() : item.Tag!.ToUpperInvariant();

    private static string GetVoteBadgeClass(VoteItemModel item)
    {
        var status = MapVoteDisplayStatus(item);
        return status switch
        {
            "Active" => "content-vote-badge--active",
            "In Production" => "content-vote-badge--production",
            "Completed" => "content-vote-badge--completed",
            _ => "content-vote-badge--accent"
        };
    }

    private static string GetVoteStatusBadgeClass(VoteItemModel item) =>
        MapVoteDisplayStatus(item) switch
        {
            "Active" => "content-status-badge--vote-active",
            "Pre-Order Ready" => "content-status-badge--vote-preorder",
            "In Production" => "content-status-badge--published",
            "Completed" => "content-status-badge--draft",
            _ => "content-status-badge--draft"
        };

    private static string GetVoteStyleLine(VoteItemModel item)
    {
        var style = string.IsNullOrWhiteSpace(item.Subtitle) ? "Apparel" : item.Subtitle!;
        var season = string.IsNullOrWhiteSpace(item.Season) ? "—" : item.Season!;
        return $"{style} · {season}";
    }

    private string GetVoteDeadline(VoteItemModel item) =>
        MapVoteDisplayStatus(item) == "Active"
            ? voteStats?.NextDeadline ?? "Open"
            : "Closed";

    private static string GetVoteLivePageUrl() => "/community";

    private void OpenVoteCampaignPanel(VoteItemModel? item = null)
    {
        ShowToast(item == null
            ? "Vote campaign creation will be available when admin write APIs are connected."
            : $"Editing \"{item.StyleName}\" will be available when admin write APIs are connected.");
    }

    private void AdvanceVoteCampaign(VoteItemModel item)
    {
        ShowToast($"Status advancement for \"{item.StyleName}\" requires admin write APIs.");
    }
}
