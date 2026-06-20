using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MuuqWear.Application.Services.VoteService;
using MuuqWear.Model.Vote;

namespace MuuqWear.Web.Components.Pages.CommunityVoteComponent;
public partial class CommunityVoteComponent
{
    private VoteRealtimeSubscription? _realtimeSub;
    // ─── REFS ─────────────────────────────────────────────────
    private ElementReference trendingRef;
    private ElementReference finishedRef;

    // ─── FILTER TABS (client-side on loaded items) ────────────
    private static readonly string[] DesignFilters =
    [
        "Upcoming Designs",
        "Latest Concepts",
        "Top Voted",
        "Pre-Order Ready"
    ];

    private string activeFilter = "Upcoming Designs";

    private IEnumerable<VoteItemModel> FilteredActiveItems => activeFilter switch
    {
        "Latest Concepts" => activeItems
            .OrderByDescending(i => i.CreatedAt ?? DateTime.MinValue),
        "Top Voted" => activeItems
            .OrderByDescending(i => i.VoteCount),
        "Pre-Order Ready" => activeItems.Where(i =>
            (!string.IsNullOrEmpty(i.Tag) &&
             i.Tag.Contains("PRE-ORDER", StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(i.Season) &&
             i.Season.Contains("Pre-Order", StringComparison.OrdinalIgnoreCase))),
        _ => activeItems
    };

    private void SelectFilter(string filter)
    {
        activeFilter = filter;
        StateHasChanged();
    }

    private List<VoteItemModel> activeItems = new();
    private List<VoteItemModel> finishedItems = new();
    private VoteStatsModel? stats;
    private bool isLoading = false;
    private bool isLoadingFinished = true;
    private bool isAuthenticated = false;

    // ─── VOTING STATE ─────────────────────────────────────────
    //  tracks which items are currently being voted on
    private HashSet<Guid> votingIds = new();
    private HashSet<Guid> preOrderingIds = new();

    // ─── COLOR SELECTION ──────────────────────────────────────
    //  tracks selected color per item
    private Dictionary<Guid, string> selectedColors = new();

    // ─── TOAST STATE ──────────────────────────────────────────
    private bool showToast = false;
    private bool toastSuccess = true;
    private string toastMessage = string.Empty;
    private CancellationTokenSource? _toastCts;

    private async Task SubscribeToVoteUpdates()
    {
        _realtimeSub = new VoteRealtimeSubscription();

        //  wire event → updates local list for this circuit
        _realtimeSub.OnVoteUpdated += HandleRealtimeVoteUpdate;

        await _realtimeSub.SubscribeAsync(
            Configuration["Supabase:Url"]!,
            Configuration["Supabase:AnonKey"]!);
    }

    //  called when any user votes → updates count on all clients
    private void HandleRealtimeVoteUpdate(Guid voteItemId, int newCount)
    {
        var item = activeItems.FirstOrDefault(i => i.Id == voteItemId);
        if (item == null) return;

        item.VoteCount = newCount;

        InvokeAsync(async () =>
        {
            await LoadStats();
            StateHasChanged();
        });
    }
    protected override async Task OnInitializedAsync()
    {
        isLoading = true;
        var authState = await AuthStateProvider
            .GetAuthenticationStateAsync();
        isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;

        //  load all data in parallel
        await Task.WhenAll(
            LoadActiveItems(),
            LoadFinishedItems(),
            LoadStats());

        isLoading = false;
        _ = SubscribeToVoteUpdates();
    }

    // ─── LOAD ─────────────────────────────────────────────────
    private async Task LoadActiveItems()
    {

        var result = isAuthenticated
        ? await VoteService.GetActiveItems()
        : await VoteService.GetActiveItemsPublic();

        if (result.Success && result.Data != null)
            activeItems = result.Data;

        InitializeColorSelections();
    }

    private void InitializeColorSelections()
    {
        foreach (var item in activeItems)
        {
            if (selectedColors.ContainsKey(item.Id) || item.ColorOptions.Count == 0)
                continue;

            if (item.HasVoted && TryResolveVotedColor(item, out var votedColor))
            {
                selectedColors[item.Id] = votedColor;
                continue;
            }

            selectedColors[item.Id] = NormalizeColorHex(item.ColorOptions[0]);
        }
    }

    private static bool TryResolveVotedColor(VoteItemModel item, out string color)
    {
        color = string.Empty;
        if (string.IsNullOrWhiteSpace(item.VotedColor))
            return false;

        var normalized = NormalizeColorHexStatic(item.VotedColor);
        if (string.IsNullOrEmpty(normalized))
            return false;

        var match = item.ColorOptions.FirstOrDefault(option =>
            string.Equals(
                NormalizeColorHexStatic(option),
                normalized,
                StringComparison.OrdinalIgnoreCase));

        color = match != null ? NormalizeColorHexStatic(match) : normalized;
        return true;
    }

    private async Task LoadFinishedItems()
    {
        try
        {
            var result = await VoteService.GetFinishedItems();
            if (result.Success && result.Data != null)
                finishedItems = result.Data;
        }
        finally
        {
            isLoadingFinished = false;
        }
    }

    private async Task LoadStats()
    {
        var result = await VoteService.GetStats();
        if (result.Success && result.Data != null)
            stats = result.Data;
    }

    // ─── REALTIME ─────────────────────────────────────────────
    //  called when any user votes → updates count on all clients

    // ─── COLOR SELECTION ──────────────────────────────────────
    private void SelectColor(Guid itemId, string color)
    {
        var item = activeItems.FirstOrDefault(i => i.Id == itemId);
        if (item?.HasVoted == true)
            return;

        selectedColors[itemId] = NormalizeColorHex(color);
        StateHasChanged();
    }

    private string? GetSelectedColor(Guid itemId)
    {
        return selectedColors.TryGetValue(itemId, out var color) ? color : null;
    }

    private bool IsColorSelected(Guid itemId, string color)
    {
        if (!selectedColors.TryGetValue(itemId, out var selected))
            return false;

        return string.Equals(
            NormalizeColorHex(selected),
            NormalizeColorHex(color),
            StringComparison.OrdinalIgnoreCase);
    }

    // ─── VOTE ─────────────────────────────────────────────────
    private async Task HandleVote(VoteItemModel item)
    {
        if (!isAuthenticated)
        {
            NavigationManager.NavigateTo(
                "/login?returnUrl=/community", forceLoad: false);
            return;
        }

        if (item.HasVoted || votingIds.Contains(item.Id)) return;

        if (!selectedColors.TryGetValue(item.Id, out var preferredColor) ||
            string.IsNullOrWhiteSpace(preferredColor))
        {
            await ShowToast("Pick a color before voting.", success: false);
            return;
        }

        preferredColor = NormalizeColorHex(preferredColor);

        votingIds.Add(item.Id);
        StateHasChanged();

        var result = await VoteService.CastVote(item.Id, preferredColor);

        votingIds.Remove(item.Id);

        if (result.Success)
        {
            item.HasVoted = true;
            item.VotedColor = preferredColor;
            selectedColors[item.Id] = preferredColor;
            item.VoteCount = result.Data?.VoteCount ?? item.VoteCount + 1;

            //  refresh stats after vote — total votes this week updates
            await LoadStats();

        }
        else
        {
            await ShowToast(
                result.Message ?? "Failed to cast vote.",
                success: false);
            return;
        }

        StateHasChanged();
    }

    // ─── PRE-ORDER ────────────────────────────────────────────
    private async Task HandlePreOrder(VoteItemModel item)
    {
        if (!isAuthenticated)
        {
            NavigationManager.NavigateTo(
                "/login?returnUrl=/community", forceLoad: false);
            return;
        }

        if (item.HasPreOrdered || preOrderingIds.Contains(item.Id)) return;

        preOrderingIds.Add(item.Id);
        StateHasChanged();

        var result = await VoteService.RegisterPreOrder(item.Id);

        preOrderingIds.Remove(item.Id);

        if (result.Success)
        {
            item.HasPreOrdered = true;
            await ShowToast(
                "Pre-order interest registered! We'll notify you first. 🛍️",
                success: true);
        }
        else
        {
            await ShowToast(
                result.Message ?? "Failed to register interest.",
                success: false);
        }

        StateHasChanged();
    }

    // ─── TOAST ────────────────────────────────────────────────
    //  single responsibility — shows toast for 3 seconds
    private async Task ShowToast(string message, bool success)
    {
        _toastCts?.Cancel();
        _toastCts = new CancellationTokenSource();

        toastMessage = message;
        toastSuccess = success;
        showToast = true;
        StateHasChanged();

        try
        {
            await Task.Delay(3000, _toastCts.Token);
            showToast = false;
            StateHasChanged();
        }
        catch (TaskCanceledException)
        {
            // new toast replaced this one — do nothing
        }
    }

    // ─── SCROLL ───────────────────────────────────────────────
    private async Task ScrollRight(ElementReference el)
    {
        await JS.InvokeVoidAsync("mwScrollRight", el, ".mw-carousel-item");
    }

    private async Task ScrollLeft(ElementReference el)
    {
        await JS.InvokeVoidAsync("mwScrollLeft", el, ".mw-carousel-item");
    }

    // ─── HELPERS ──────────────────────────────────────────────
    private string GetActiveBadgeClass(string? tag)
    {
        if (string.Equals(tag, "VOTE NOW", StringComparison.OrdinalIgnoreCase))
            return "mw-card__badge--vote-now";

        return "mw-card__badge--trending";
    }

    private static string NormalizeColorHex(string color) =>
        NormalizeColorHexStatic(color);

    private static string NormalizeColorHexStatic(string color)
    {
        var trimmed = color.Trim();
        if (trimmed.Length == 0)
            return trimmed;

        if (!trimmed.StartsWith('#') &&
            trimmed.Length == 6 &&
            trimmed.All(Uri.IsHexDigit))
        {
            return "#" + trimmed.ToUpperInvariant();
        }

        if (trimmed.StartsWith('#') && trimmed.Length == 7)
            return "#" + trimmed[1..].ToUpperInvariant();

        return trimmed;
    }

    private static bool TryGetColorHex(string? value, out string hex)
    {
        hex = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (!trimmed.StartsWith('#'))
        {
            if (trimmed.Length == 6 && trimmed.All(Uri.IsHexDigit))
            {
                hex = $"#{trimmed}";
                return true;
            }

            return false;
        }

        var digits = trimmed[1..];
        if (digits.Length == 3 &&
            digits.All(c => Uri.IsHexDigit(c)))
        {
            hex = $"#{digits[0]}{digits[0]}{digits[1]}{digits[1]}{digits[2]}{digits[2]}";
            return true;
        }

        if (digits.Length == 6 &&
            digits.All(c => Uri.IsHexDigit(c)))
        {
            hex = trimmed;
            return true;
        }

        return false;
    }

    private string GetFinishedBadgeClass(string? status) => status switch
    {
        "production" => "mw-finished-card__badge--production",
        _ => "mw-finished-card__badge--winner"
    };

    // ─── CLEANUP ──────────────────────────────────────────────
    //  unsubscribe on dispose — prevents memory leaks
    public async ValueTask DisposeAsync()
    {
        _toastCts?.Cancel();
        _toastCts?.Dispose();

        if (_realtimeSub != null)
        {
            //  unsubscribe event first
            _realtimeSub.OnVoteUpdated -= HandleRealtimeVoteUpdate;

            //  then dispose subscription
            await _realtimeSub.DisposeAsync();
        }
    }
}
