using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using MuuqWear.Application.Services.AdminBadgeService;
using MuuqWear.Application.Services.AffiliateService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.AffiliateApplication;
using MuuqWear.Model.AdminBadgeCount;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminAffiliatesComponent : IDisposable
{
    internal const int MaxActiveAffiliates = 500;

    [Inject] private IAffiliateService AffiliateService { get; set; } = default!;
    [Inject] private IAdminBadgeService BadgeService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AdminAffiliatesTabCoordinator AffiliatesTabCoordinator { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "tab")]
    public string? TabQuery { get; set; }

    private string activeTab = "pending";

    private List<AffiliateApplicationModel> pendingApplications = new();
    private List<AffiliateApplicationModel> activeApplications = new();
    private List<AffiliateApplicationModel> waitListApplications = new();
    private List<AffiliateTierModel> affiliateTiers = new();
    private Dictionary<string, TierEditForm> tierEdits = new(StringComparer.OrdinalIgnoreCase);
    private List<AffiliatePendingPayoutModel> pendingPayouts = new();
    private List<AffiliatePayoutResultModel> payoutHistory = new();
    private string payoutView = "pending";
    private int payoutHistoryPage = 1;
    private int payoutHistoryTotalPages;
    private bool payoutHistoryHasNext;
    private bool payoutHistoryHasPrevious;
    private AffiliateCountsModel affiliateCounts = new();
    private AffiliateAdminStatsModel? adminStats;

    private bool showViewModal;
    private AffiliateApplicationModel? selectedApplication;
    private bool isLoading = true;
    private bool isLoadingTabContent;
    private bool hasLoadedOnce;
    private bool isActionInProgress;
    private string? processingPayoutCode;
    private string? errorMessage;
    private string? loadedCacheKey;

    private AffiliatePendingPayoutModel? payoutToConfirm;
    private string? expandedPayoutCode;
    private List<AffiliatePendingReferralModel> expandedReferrals = new();
    private bool isLoadingReferrals;
    private List<AffiliateApplicationModel> allApplications = new();
    private string pendingSearch = string.Empty;
    private string pendingStatusFilter = "pending";
    private string activeSearch = string.Empty;
    private string activeTierFilter = "All";
    private bool activeShowInactiveOnly;
    private string? toastMessage;
    private Guid? undoApplicationId;
    private string? editingTierSlug;
    private bool showProcessAllConfirm;
    private bool showTierResetConfirm;
    private bool tierHasUnsavedChanges;

    private CancellationTokenSource? toastCts;
    private int _loadGate;

    private static readonly (string Tab, string Label)[] TabViews =
    {
        ("pending", "Pending Applications"),
        ("active", "Active Affiliates"),
        ("payouts", "Payouts"),
        ("tiers", "Tier Settings")
    };

    protected override void OnInitialized()
    {
        AffiliatesTabCoordinator.TabChanged += OnAffiliatesTabChanged;
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        ApplyTabFromQuery();

        if (hasLoadedOnce && loadedCacheKey != GetTabCacheKey())
            isLoadingTabContent = true;

        await LoadTabDataAsync();
    }

    public void Dispose()
    {
        AffiliatesTabCoordinator.TabChanged -= OnAffiliatesTabChanged;
        NavigationManager.LocationChanged -= OnLocationChanged;
        toastCts?.Cancel();
        toastCts?.Dispose();
    }

    private void OnAffiliatesTabChanged(string tab)
    {
        var normalized = AdminAffiliatesTabCoordinator.NormalizeTab(tab);
        if (activeTab == normalized && loadedCacheKey == GetTabCacheKey())
            return;

        activeTab = normalized;
        loadedCacheKey = null;

        _ = InvokeAsync(async () =>
        {
            await LoadTabDataAsync();
            StateHasChanged();
        });
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (!NavigationManager.ToBaseRelativePath(NavigationManager.Uri)
                .StartsWith("admin/affiliates", StringComparison.OrdinalIgnoreCase))
            return;

        _ = InvokeAsync(async () =>
        {
            ApplyTabFromQuery();

            if (hasLoadedOnce && loadedCacheKey != GetTabCacheKey())
                isLoadingTabContent = true;

            await LoadTabDataAsync();
            StateHasChanged();
        });
    }

    private void ApplyTabFromQuery()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        var tab = query.TryGetValue("tab", out var value) && !string.IsNullOrEmpty(value)
            ? value.ToString()
            : TabQuery;

        activeTab = AdminAffiliatesTabCoordinator.NormalizeTab(tab);
    }

    private void SwitchTab(string tab)
    {
        var normalized = AdminAffiliatesTabCoordinator.NormalizeTab(tab);
        if (activeTab == normalized)
            return;

        NavigationManager.NavigateTo($"/admin/affiliates?tab={normalized}");
    }

    private void SetPendingFilter(string filter) => pendingStatusFilter = filter;

    private void SetPayoutView(string view)
    {
        if (payoutView == view)
            return;

        payoutView = view;
        if (view == "history")
            payoutHistoryPage = 1;

        loadedCacheKey = null;
        _ = LoadTabDataAsync();
    }

    private string GetTabCacheKey() =>
        activeTab == "payouts"
            ? $"{activeTab}:{payoutView}:{payoutHistoryPage}"
            : activeTab;

    private bool IsCurrentTabLoading =>
        isLoading || isLoadingTabContent || loadedCacheKey != GetTabCacheKey();

    private IEnumerable<AffiliateApplicationModel> FilteredPendingApplications
    {
        get
        {
            var q = pendingSearch.Trim();
            return allApplications.Where(a =>
            {
                if (!string.IsNullOrEmpty(q))
                {
                    var handle = AffiliateAdminDesignHelper.GetPrimaryHandle(a.SocialHandles);
                    if (!a.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) &&
                        !handle.Contains(q, StringComparison.OrdinalIgnoreCase) &&
                        !a.ContentNiche.Contains(q, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                if (pendingStatusFilter == "all")
                    return true;

                var status = a.Status.ToLowerInvariant();
                return pendingStatusFilter switch
                {
                    "pending" => status == "pending",
                    "waitlisted" => status == "waitlisted",
                    "approved" => status == "approved",
                    "denied" => status is "rejected" or "denied",
                    _ => true
                };
            });
        }
    }

    private IEnumerable<AffiliateApplicationModel> FilteredActiveApplications
    {
        get
        {
            var q = activeSearch.Trim();
            return activeApplications.Where(a =>
            {
                if (!string.IsNullOrEmpty(q))
                {
                    var handle = AffiliateAdminDesignHelper.GetPrimaryHandle(a.SocialHandles);
                    if (!a.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) &&
                        !handle.Contains(q, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                var tierSlug = a.AffiliateTier ?? string.Empty;
                var tierLabel = AffiliateAdminDesignHelper.FormatTierLabel(tierSlug);
                if (activeTierFilter != "All" &&
                    !activeTierFilter.Equals(tierSlug, StringComparison.OrdinalIgnoreCase) &&
                    !activeTierFilter.Equals(tierLabel, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (activeShowInactiveOnly)
                    return !a.IsActive;

                return a.IsActive;
            });
        }
    }

    private int PendingReviewCount => affiliateCounts.Pending > 0
        ? affiliateCounts.Pending
        : pendingApplications.Count;

    private int WaitlistedCount => affiliateCounts.Waitlisted > 0
        ? affiliateCounts.Waitlisted
        : waitListApplications.Count;

    private int ApplicationsThisMonth => allApplications.Count(a =>
        a.SubmittedAt.Month == DateTime.UtcNow.Month &&
        a.SubmittedAt.Year == DateTime.UtcNow.Year);

    private decimal TotalPendingPayoutAmount => adminStats?.PendingPayoutAmount
        ?? pendingPayouts.Sum(p => p.TotalAmount);

    private int PendingPayoutCountDisplay => adminStats?.PendingPayoutCount ?? pendingPayouts.Count;

    private decimal ProcessedThisMonthAmount => adminStats?.ProcessedThisMonthAmount
        ?? payoutHistory.Where(p =>
            p.ProcessedAt.Month == DateTime.UtcNow.Month &&
            p.ProcessedAt.Year == DateTime.UtcNow.Year).Sum(p => p.TotalAmount);

    private int ProcessedThisMonthCount => adminStats?.ProcessedThisMonthCount
        ?? payoutHistory.Count(p =>
            p.ProcessedAt.Month == DateTime.UtcNow.Month &&
            p.ProcessedAt.Year == DateTime.UtcNow.Year);

    private decimal TotalDisbursedYtd => adminStats?.TotalDisbursedYtd
        ?? payoutHistory.Sum(p => p.TotalAmount);

    private decimal TotalCommissionsPaid => adminStats?.TotalCommissionsPaid ?? 0;
    private int TotalItemsSold => adminStats?.TotalItemsSold ?? 0;
    private int ActiveAffiliateCountDisplay => adminStats?.ActiveAffiliates ?? activeApplications.Count(a => a.IsActive);

    private int GetTierAffiliateCount(string tierKey)
    {
        if (adminStats?.AffiliatesByTier != null)
        {
            var match = adminStats.AffiliatesByTier.FirstOrDefault(kv =>
                kv.Key.Equals(tierKey, StringComparison.OrdinalIgnoreCase) ||
                AffiliateAdminDesignHelper.FormatTierLabel(kv.Key)
                    .Equals(tierKey, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(match.Key))
                return match.Value;
        }

        var tier = affiliateTiers.FirstOrDefault(t =>
            t.DisplayName.Equals(tierKey, StringComparison.OrdinalIgnoreCase) ||
            t.Slug.Equals(tierKey, StringComparison.OrdinalIgnoreCase));
        if (tier != null)
            return tier.CurrentAffiliateCount;

        return activeApplications.Count(a =>
            AffiliateAdminDesignHelper.FormatTierLabel(a.AffiliateTier)
                .Equals(tierKey, StringComparison.OrdinalIgnoreCase));
    }

    private IEnumerable<(string Key, string Label)> GetTierFilterOptions()
    {
        if (adminStats?.AffiliatesByTier?.Count > 0)
        {
            return AffiliateTierCatalog.AggregateCanonicalCounts(adminStats.AffiliatesByTier)
                .OrderBy(kv => AffiliateTierCatalog.DefaultTiers
                    .FirstOrDefault(t => t.Slug.Equals(kv.Key, StringComparison.OrdinalIgnoreCase))?.SortOrder ?? 99)
                .Select(kv => (kv.Key, AffiliateAdminDesignHelper.FormatTierLabel(kv.Key)));
        }

        if (affiliateTiers.Count > 0)
        {
            return affiliateTiers
                .OrderBy(t => t.SortOrder)
                .Select(t => (t.Slug, t.DisplayName));
        }

        return AffiliateAdminDesignHelper.TierThemes.Select(t => (t.Name.ToLowerInvariant(), t.Name));
    }

    private IEnumerable<(string Key, string Label, int Count)> GetTierStatCards()
    {
        if (adminStats?.AffiliatesByTier?.Count > 0)
        {
            return AffiliateTierCatalog.AggregateCanonicalCounts(adminStats.AffiliatesByTier)
                .OrderBy(kv => AffiliateTierCatalog.DefaultTiers
                    .FirstOrDefault(t => t.Slug.Equals(kv.Key, StringComparison.OrdinalIgnoreCase))?.SortOrder ?? 99)
                .Select(kv => (kv.Key, AffiliateAdminDesignHelper.FormatTierLabel(kv.Key), kv.Value));
        }

        if (affiliateTiers.Count > 0)
        {
            return affiliateTiers
                .OrderBy(t => t.SortOrder)
                .Select(t => (t.Slug, t.DisplayName, t.CurrentAffiliateCount));
        }

        return AffiliateAdminDesignHelper.TierThemes
            .Select(t => (t.Name.ToLowerInvariant(), t.Name, GetTierAffiliateCount(t.Name)));
    }

    private async Task ShowToastAsync(string message)
    {
        toastCts?.Cancel();
        toastCts?.Dispose();
        toastCts = new CancellationTokenSource();
        toastMessage = message;
        StateHasChanged();

        try
        {
            await Task.Delay(2400, toastCts.Token);
            toastMessage = null;
            await InvokeAsync(StateHasChanged);
        }
        catch (TaskCanceledException)
        {
        }
    }

    private void StartTierEdit(string slug)
    {
        if (!tierEdits.ContainsKey(slug))
            return;

        editingTierSlug = slug;
        tierHasUnsavedChanges = false;
    }

    private void CancelTierEdit()
    {
        if (editingTierSlug != null &&
            affiliateTiers.FirstOrDefault(t =>
                t.Slug.Equals(editingTierSlug, StringComparison.OrdinalIgnoreCase)) is { } tier)
        {
            tierEdits[editingTierSlug] = TierEditForm.FromTier(tier);
        }

        editingTierSlug = null;
        tierHasUnsavedChanges = false;
    }

    private void MarkTierDirty() => tierHasUnsavedChanges = true;

    private async Task SaveEditingTierAsync()
    {
        if (string.IsNullOrEmpty(editingTierSlug))
            return;

        var slug = editingTierSlug;
        await SaveTierAsync(slug);
        if (tierEdits.TryGetValue(slug, out var edit) && string.IsNullOrEmpty(edit.Error))
        {
            var label = AffiliateAdminDesignHelper.FormatTierLabel(slug);
            editingTierSlug = null;
            tierHasUnsavedChanges = false;
            await ShowToastAsync($"{label} tier settings saved");
        }
    }

    private void ResetTierFormsToLoaded()
    {
        tierEdits = affiliateTiers.ToDictionary(
            t => t.Slug,
            t => TierEditForm.FromTier(t),
            StringComparer.OrdinalIgnoreCase);
        editingTierSlug = null;
        tierHasUnsavedChanges = false;
    }

    private async Task ProcessAllPayoutsAsync()
    {
        showProcessAllConfirm = false;
        isActionInProgress = true;
        StateHasChanged();

        try
        {
            var result = await AffiliateService.ProcessAllAdminPayouts(new ProcessAllAffiliatePayoutsModel
            {
                PaymentMethod = "manual"
            });

            if (result.Success && result.Data != null)
            {
                await RefreshAfterMutationAsync(MutationScope.Payout);
                await ShowToastAsync(
                    $"{result.Data.ProcessedCount} payouts processed — ${result.Data.TotalAmount:N0} disbursed");
            }
            else
            {
                errorMessage = result.Message ?? "Failed to process all payouts.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = "Failed to process all payouts.";
            Console.WriteLine($"[AdminAffiliates] ProcessAllPayouts error: {ex.Message}");
        }
        finally
        {
            isActionInProgress = false;
            StateHasChanged();
        }
    }

    private async Task ToggleAffiliateActiveAsync(Guid userId, bool isActive)
    {
        if (isActionInProgress)
            return;

        isActionInProgress = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            var result = await AffiliateService.UpdateAffiliateActiveStatus(userId,
                new UpdateAffiliateActiveStatusModel { IsActive = isActive });

            if (result.Success)
            {
                await RefreshAfterMutationAsync(MutationScope.ActiveAffiliate);
                await ShowToastAsync(isActive ? "Affiliate activated" : "Affiliate deactivated");
            }
            else
            {
                errorMessage = result.Message ?? "Failed to update affiliate status.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = "Failed to update affiliate status.";
            Console.WriteLine($"[AdminAffiliates] ToggleAffiliateActive error: {ex.Message}");
        }
        finally
        {
            isActionInProgress = false;
            StateHasChanged();
        }
    }

    private void AddTierPerk(string slug)
    {
        if (!tierEdits.TryGetValue(slug, out var edit) || string.IsNullOrWhiteSpace(edit.NewPerk))
            return;

        edit.Perks.Add(edit.NewPerk.Trim());
        edit.NewPerk = string.Empty;
        MarkTierDirty();
    }

    private void RemoveTierPerk(string slug, int index)
    {
        if (!tierEdits.TryGetValue(slug, out var edit) || index < 0 || index >= edit.Perks.Count)
            return;

        edit.Perks.RemoveAt(index);
        MarkTierDirty();
    }

    private async Task UndoLastApplicationActionAsync(Guid applicationId)
    {
        undoApplicationId = null;
        var result = await UpdateStatusAsync(applicationId, "pending");
        if (result.Success)
        {
            await RefreshAfterMutationAsync(MutationScope.PendingApplication);
            await ShowToastAsync("Action undone");
        }
    }

    private static (string Bg, string Fg) GetTierBadgeColors(string slug) =>
        AffiliateAdminDesignHelper.GetTierTheme(slug) switch
        {
            var t => (t.BgColor, t.TextColor)
        };

    private enum MutationScope
    {
        PendingApplication,
        ActiveAffiliate,
        Payout,
        Tier
    }

    private async Task LoadTabDataAsync(bool force = false)
    {
        if (!force && loadedCacheKey == GetTabCacheKey())
            return;

        if (Interlocked.CompareExchange(ref _loadGate, 1, 0) != 0)
            return;

        if (!hasLoadedOnce)
            isLoading = true;
        else
            isLoadingTabContent = true;

        errorMessage = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            await LoadTabDataCoreAsync();
        }
        catch (Exception ex)
        {
            errorMessage = "Failed to load affiliate data.";
            Console.WriteLine($"[AdminAffiliates] Load error: {ex.Message}");
        }
        finally
        {
            isLoading = false;
            isLoadingTabContent = false;
            hasLoadedOnce = true;
            loadedCacheKey = GetTabCacheKey();
            Interlocked.Exchange(ref _loadGate, 0);
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadTabDataCoreAsync()
    {
        var tasks = new List<Task> { LoadBadgeCountsAsync() };

        switch (activeTab)
        {
            case "pending":
                tasks.Add(LoadPendingApplicationsAsync());
                break;
            case "active":
                tasks.Add(LoadAdminStatsAsync());
                tasks.Add(LoadActiveApplicationsAsync());
                break;
            case "payouts":
                tasks.Add(LoadAdminStatsAsync());
                tasks.Add(LoadPayoutsAsync());
                break;
            case "tiers":
                tasks.Add(LoadTiersAsync());
                break;
        }

        await Task.WhenAll(tasks);
    }

    private async Task RefreshAfterMutationAsync(MutationScope scope)
    {
        isLoadingTabContent = true;
        errorMessage = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            var tasks = new List<Task> { LoadBadgeCountsAsync() };

            switch (scope)
            {
                case MutationScope.PendingApplication:
                    tasks.Add(LoadPendingApplicationsAsync());
                    break;
                case MutationScope.ActiveAffiliate:
                    tasks.Add(LoadAdminStatsAsync());
                    tasks.Add(LoadActiveApplicationsAsync());
                    break;
                case MutationScope.Payout:
                    tasks.Add(LoadAdminStatsAsync());
                    tasks.Add(LoadPayoutsAsync());
                    break;
                case MutationScope.Tier:
                    tasks.Add(LoadTiersAsync());
                    break;
            }

            await Task.WhenAll(tasks);
            loadedCacheKey = GetTabCacheKey();
        }
        catch (Exception ex)
        {
            errorMessage = "Failed to refresh affiliate data.";
            Console.WriteLine($"[AdminAffiliates] Refresh error: {ex.Message}");
        }
        finally
        {
            isLoadingTabContent = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadAdminStatsAsync()
    {
        var result = await AffiliateService.GetAdminStats();
        if (result.Success && result.Data != null)
            adminStats = result.Data;
    }

    private async Task LoadBadgeCountsAsync()
    {
        var result = await BadgeService.GetCounts();
        if (result.Success && result.Data != null)
            affiliateCounts = result.Data.AffiliateCounts;
    }

    private async Task LoadPendingApplicationsAsync()
    {
        var pendingTask = AffiliateService.GetAllApplications("pending");
        var waitlistTask = AffiliateService.GetAllApplications("waitlisted");
        var approvedTask = AffiliateService.GetAllApplications("approved");
        var rejectedTask = AffiliateService.GetAllApplications("rejected");

        await Task.WhenAll(pendingTask, waitlistTask, approvedTask, rejectedTask);

        pendingApplications = GetApplicationList(await pendingTask);
        waitListApplications = GetApplicationList(await waitlistTask);
        var approved = GetApplicationList(await approvedTask);
        var rejected = GetApplicationList(await rejectedTask);

        allApplications = pendingApplications
            .Concat(waitListApplications)
            .Concat(approved)
            .Concat(rejected)
            .ToList();
    }

    private async Task LoadActiveApplicationsAsync()
    {
        var result = await AffiliateService.GetAllApplications("approved");
        if (result.Success && result.Data != null)
            activeApplications = result.Data;
    }

    private static List<AffiliateApplicationModel> GetApplicationList(
        Response<List<AffiliateApplicationModel>> result) =>
        result.Success && result.Data != null ? result.Data : new List<AffiliateApplicationModel>();

    private async Task LoadPayoutsAsync()
    {
        if (payoutView == "history")
        {
            await LoadPayoutHistoryAsync();
            return;
        }

        var result = await AffiliateService.GetAdminPendingPayouts();
        if (!result.Success || result.Data == null)
        {
            errorMessage = result.Message ?? "Failed to load pending payouts.";
            pendingPayouts = new();
        }
        else
        {
            pendingPayouts = result.Data;
        }
    }

    private async Task LoadPayoutHistoryAsync()
    {
        const int pageSize = 20;
        var result = await AffiliateService.GetAdminPayoutHistory(payoutHistoryPage, pageSize);
        if (!result.Success || result.Data == null)
        {
            errorMessage = result.Message ?? "Failed to load payout history.";
            payoutHistory = new();
            payoutHistoryTotalPages = 0;
            payoutHistoryHasNext = false;
            payoutHistoryHasPrevious = false;
            return;
        }

        payoutHistory = result.Data.Data;
        payoutHistoryTotalPages = result.Data.TotalPages;
        payoutHistoryHasNext = result.Data.HasNextPage;
        payoutHistoryHasPrevious = result.Data.HasPreviousPage;
    }

    private async Task GoToPayoutHistoryPage(int page)
    {
        if (page < 1 || (payoutHistoryTotalPages > 0 && page > payoutHistoryTotalPages))
            return;

        payoutHistoryPage = page;
        loadedCacheKey = null;
        await LoadTabDataAsync(force: true);
    }

    private void OpenPayoutConfirm(AffiliatePendingPayoutModel payout)
    {
        payoutToConfirm = payout;
    }

    private void ClosePayoutConfirm()
    {
        payoutToConfirm = null;
    }

    private async Task ConfirmProcessPayoutAsync()
    {
        if (payoutToConfirm == null)
            return;

        var code = payoutToConfirm.AffiliateCode;
        ClosePayoutConfirm();
        await ProcessPayoutAsync(code);
    }

    private async Task TogglePayoutDetailsAsync(string affiliateCode)
    {
        if (expandedPayoutCode == affiliateCode)
        {
            expandedPayoutCode = null;
            expandedReferrals = new();
            return;
        }

        expandedPayoutCode = affiliateCode;
        expandedReferrals = new();
        isLoadingReferrals = true;
        StateHasChanged();

        try
        {
            var result = await AffiliateService.GetAdminPendingReferrals(affiliateCode);
            if (result.Success && result.Data != null)
                expandedReferrals = result.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminAffiliates] TogglePayoutDetails error: {ex.Message}");
        }
        finally
        {
            isLoadingReferrals = false;
            StateHasChanged();
        }
    }

    private async Task ProcessPayoutAsync(string affiliateCode)
    {
        if (!string.IsNullOrEmpty(processingPayoutCode))
            return;

        processingPayoutCode = affiliateCode;
        errorMessage = null;
        StateHasChanged();

        try
        {
            var result = await AffiliateService.ProcessAdminPayout(affiliateCode, new ProcessAffiliatePayoutModel
            {
                PaymentMethod = "manual"
            });

            if (result.Success)
            {
                expandedPayoutCode = null;
                expandedReferrals = new();
                await RefreshAfterMutationAsync(MutationScope.Payout);
                await ShowToastAsync("Payout processed successfully");
            }
            else
            {
                errorMessage = result.Message ?? "Failed to process payout.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = "Failed to process payout.";
            Console.WriteLine($"[AdminAffiliates] ProcessPayout error: {ex.Message}");
        }
        finally
        {
            processingPayoutCode = null;
            StateHasChanged();
        }
    }

    private async Task LoadTiersAsync()
    {
        var result = await AffiliateService.GetAdminTiers();
        if (!result.Success || result.Data == null)
        {
            errorMessage = result.Message ?? "Failed to load tier settings.";
            affiliateTiers = new();
            tierEdits = new(StringComparer.OrdinalIgnoreCase);
            return;
        }

        affiliateTiers = AffiliateTierCatalog.FilterCanonical(result.Data);
        tierEdits = affiliateTiers.ToDictionary(
            t => t.Slug,
            t => TierEditForm.FromTier(t),
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task SaveTierAsync(string slug)
    {
        if (!tierEdits.TryGetValue(slug, out var edit) || edit.IsSaving)
            return;

        edit.Error = null;
        edit.Success = null;

        if (!TryValidateTierEdit(slug, edit, out var validationError))
        {
            edit.Error = validationError;
            StateHasChanged();
            return;
        }

        edit.IsSaving = true;
        StateHasChanged();

        try
        {
            var result = await AffiliateService.UpdateAdminTier(slug, new UpdateAffiliateTierModel
            {
                ItemsSoldThreshold = edit.ItemsSoldThreshold,
                CommissionRatePercent = edit.CommissionRatePercent,
                ReferralDiscountPercent = edit.ReferralDiscountPercent,
                QuarterlyBonusPercent = edit.QuarterlyBonusPercent,
                MaxAffiliates = edit.MaxAffiliates,
                Perks = edit.Perks.ToList(),
                IsActive = edit.IsActive
            });

            if (result.Success && result.Data != null)
            {
                var index = affiliateTiers.FindIndex(t =>
                    t.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                    affiliateTiers[index] = result.Data;

                tierEdits[slug] = TierEditForm.FromTier(result.Data);
                tierEdits[slug].Success = "Saved.";
            }
            else
            {
                edit.Error = result.Message ?? "Failed to save tier.";
            }
        }
        catch (Exception ex)
        {
            edit.Error = "Failed to save tier.";
            Console.WriteLine($"[AdminAffiliates] SaveTier error: {ex.Message}");
        }
        finally
        {
            edit.IsSaving = false;
            StateHasChanged();
        }
    }

    private bool TryValidateTierEdit(string slug, TierEditForm edit, out string? error)
    {
        error = null;

        if (edit.CommissionRatePercent is < 0 or > 100)
        {
            error = "Commission rate must be between 0 and 100.";
            return false;
        }

        if (edit.ReferralDiscountPercent is < 0 or > 100)
        {
            error = "Referral discount must be between 0 and 100.";
            return false;
        }

        if (edit.QuarterlyBonusPercent is < 0 or > 100)
        {
            error = "Quarterly bonus must be between 0 and 100.";
            return false;
        }

        if (edit.ItemsSoldThreshold < 0)
        {
            error = "Items sold threshold cannot be negative.";
            return false;
        }

        var activeThresholds = affiliateTiers
            .OrderBy(t => t.SortOrder)
            .Where(t =>
            {
                if (!tierEdits.TryGetValue(t.Slug, out var form))
                    return t.IsActive;

                return t.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)
                    ? edit.IsActive
                    : form.IsActive;
            })
            .Select(t =>
            {
                if (tierEdits.TryGetValue(t.Slug, out var form))
                    return form.ItemsSoldThreshold;

                return t.ItemsSoldThreshold;
            })
            .ToList();

        for (var i = 1; i < activeThresholds.Count; i++)
        {
            if (activeThresholds[i] <= activeThresholds[i - 1])
            {
                error = "Items sold thresholds must increase for each active tier.";
                return false;
            }
        }

        if (slug.Equals("bronze", StringComparison.OrdinalIgnoreCase) && !edit.IsActive)
        {
            error = "Bronze tier cannot be deactivated.";
            return false;
        }

        if (activeThresholds.Count == 0)
        {
            error = "At least one tier must remain active.";
            return false;
        }

        return true;
    }

    private sealed class TierEditForm
    {
        public int ItemsSoldThreshold { get; set; }
        public decimal CommissionRatePercent { get; set; }
        public decimal ReferralDiscountPercent { get; set; }
        public decimal QuarterlyBonusPercent { get; set; }
        public int? MaxAffiliates { get; set; }
        public List<string> Perks { get; set; } = new();
        public string NewPerk { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsSaving { get; set; }
        public string? Error { get; set; }
        public string? Success { get; set; }

        public static TierEditForm FromTier(AffiliateTierModel tier) => new()
        {
            ItemsSoldThreshold = tier.ItemsSoldThreshold,
            CommissionRatePercent = tier.CommissionRatePercent,
            ReferralDiscountPercent = tier.ReferralDiscountPercent,
            QuarterlyBonusPercent = tier.QuarterlyBonusPercent,
            MaxAffiliates = tier.MaxAffiliates,
            Perks = tier.Perks?.ToList() ?? new List<string>(),
            IsActive = tier.IsActive
        };
    }

    private async Task HandleAction((Guid Id, string Action) data)
    {
        var (applicationId, action) = data;

        if (action.Equals("view", StringComparison.OrdinalIgnoreCase))
        {
            OpenViewModal(applicationId);
            return;
        }

        if (action.Equals("toggle-active", StringComparison.OrdinalIgnoreCase))
        {
            var app = activeApplications.FirstOrDefault(a => a.Id == applicationId);
            if (app != null)
                await ToggleAffiliateActiveAsync(app.UserId, !app.IsActive);
            return;
        }

        if (isActionInProgress)
            return;

        isActionInProgress = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            MuuqWear.Application.Shared.Response<AffiliateApplicationModel>? result = action.ToLowerInvariant() switch
            {
                "approve" => await ApproveAsync(applicationId),
                "reject" => await UpdateStatusAsync(applicationId, "rejected"),
                "waitlist" => await UpdateStatusAsync(applicationId, "waitlisted"),
                _ => null
            };

            if (result == null)
            {
                errorMessage = "Unknown action.";
                return;
            }

            if (result.Success)
            {
                await RefreshAfterMutationAsync(MutationScope.PendingApplication);
                await ShowToastAsync(action.ToLowerInvariant() switch
                {
                    "approve" => "Application approved",
                    "reject" => "Application denied",
                    "waitlist" => "Added to waitlist",
                    _ => "Application updated"
                });
                undoApplicationId = applicationId;
            }
            else
            {
                errorMessage = result.Message ?? "Action failed.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = "Failed to update application.";
            Console.WriteLine($"[AdminAffiliates] HandleAction error: {ex.Message}");
        }
        finally
        {
            isActionInProgress = false;
            StateHasChanged();
        }
    }

    private async Task<Response<AffiliateApplicationModel>?> ApproveAsync(Guid applicationId)
    {
        var approveResult = await AffiliateService.ApproveApplication(applicationId);
        if (!approveResult.Success)
            return Response<AffiliateApplicationModel>.Fail(approveResult.Message ?? "Approval failed");

        return Response<AffiliateApplicationModel>.SuccessResponse(new AffiliateApplicationModel());
    }

    private Task<Response<AffiliateApplicationModel>> UpdateStatusAsync(Guid applicationId, string status) =>
        AffiliateService.UpdateApplicationStatus(applicationId, new UpdateAffiliateApplicationStatusModel
        {
            Status = status
        });

    private void OpenViewModal(Guid applicationId)
    {
        selectedApplication = allApplications.FirstOrDefault(a => a.Id == applicationId)
            ?? activeApplications.FirstOrDefault(a => a.Id == applicationId);

        if (selectedApplication != null)
            showViewModal = true;
    }

    private void CloseViewModal()
    {
        showViewModal = false;
        selectedApplication = null;
    }

    private async Task ConfirmResetTiersAsync()
    {
        showTierResetConfirm = false;
        await LoadTiersAsync();
        ResetTierFormsToLoaded();
        await ShowToastAsync("Tiers reset to defaults");
    }

    private string FormatTierLabel(string tier) => AffiliateAdminDesignHelper.FormatTierLabel(tier);

    private int GetTabCount(string tab) => tab switch
    {
        "pending" => affiliateCounts.Pending > 0 ? affiliateCounts.Pending : pendingApplications.Count,
        "active" => affiliateCounts.Approved > 0 ? affiliateCounts.Approved : activeApplications.Count,
        "payouts" => affiliateCounts.PendingPayouts > 0
            ? affiliateCounts.PendingPayouts
            : pendingPayouts.Count,
        _ => 0
    };

    private int PendingPayoutCount => GetTabCount("payouts");

    private int ActiveAffiliateCount =>
        affiliateCounts.Approved > 0 ? affiliateCounts.Approved : activeApplications.Count;

    private string GetPageSubtitle() => activeTab switch
    {
        "active" => "Manage active affiliate accounts",
        "payouts" => "Process affiliate commission payouts",
        "tiers" => "Configure affiliate tier thresholds and commission rates",
        _ => "Review and approve affiliate applications"
    };

    private string GetTabHeading() => activeTab switch
    {
        "active" => $"Active Affiliates ({ActiveAffiliateCount}/{MaxActiveAffiliates})",
        "payouts" => payoutView == "history"
            ? "Payout history"
            : $"Payouts ({PendingPayoutCount})",
        "tiers" => "Tier Settings",
        _ => $"Pending Applications ({GetTabCount("pending")})"
    };
}
