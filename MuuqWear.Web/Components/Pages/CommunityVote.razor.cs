using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MuuqWear.Application.Services.VoteService;
using MuuqWear.Model.Vote;

namespace MuuqWear.Web.Components.Pages
{
    public partial class CommunityVote
    {
        private VoteRealtimeSubscription? _realtimeSub;
        // ─── REFS ─────────────────────────────────────────────────
        private ElementReference trendingRef;
        private ElementReference finishedRef;

        // ─── STATE ────────────────────────────────────────────────
        private List<VoteItemModel> activeItems = new();
        private List<VoteItemModel> finishedItems = new();
        private VoteStatsModel? stats;
        private bool isLoadingActive = true;
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

            //  InvokeAsync — realtime fires on background thread
            InvokeAsync(StateHasChanged);
        }
        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider
                .GetAuthenticationStateAsync();
            isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;

            //  load all data in parallel
            await Task.WhenAll(
                LoadActiveItems(),
                LoadFinishedItems(),
                LoadStats());

            //  subscribe after data loaded
            await SubscribeToVoteUpdates();
        }

        // ─── LOAD ─────────────────────────────────────────────────
        private async Task LoadActiveItems()
        {
            isLoadingActive = true;

            var result = isAuthenticated
            ? await VoteService.GetActiveItems()
            : await VoteService.GetActiveItemsPublic();

            if (result.Success && result.Data != null)
                activeItems = result.Data;
            foreach (var item in activeItems)
                System.Diagnostics.Debug.WriteLine($"{item.StyleName} → ImageUrl: {item.ImageUrl}");

            isLoadingActive = false;
        }

        private async Task LoadFinishedItems()
        {
            isLoadingFinished = true;
            var result = await VoteService.GetFinishedItems();
            if (result.Success && result.Data != null)
                finishedItems = result.Data;
            isLoadingFinished = false;
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
            selectedColors[itemId] = color;
            StateHasChanged();
        }

        //  gets selected color for item or first color as default
        private string? GetSelectedColor(Guid itemId)
        {
            if (selectedColors.TryGetValue(itemId, out var color))
                return color;
            return null;
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

            votingIds.Add(item.Id);
            StateHasChanged();

            var result = await VoteService.CastVote(item.Id);

            votingIds.Remove(item.Id);

            if (result.Success)
            {
                item.HasVoted = true;
                item.VoteCount = result.Data?.VoteCount ?? item.VoteCount + 1;

                //  refresh stats after vote — total votes this week updates
                await LoadStats();

                await ShowToast("Your vote has been cast! 🎉", success: true);
            }
            else
            {
                await ShowToast(
                    result.Message ?? "Failed to cast vote.",
                    success: false);
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
            await JS.InvokeVoidAsync("mwScrollRight", el, ".mw-card");
        }

        private async Task ScrollLeft(ElementReference el)
        {
            await JS.InvokeVoidAsync("mwScrollLeft", el, ".mw-card");
        }

        // ─── HELPERS ──────────────────────────────────────────────
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
}