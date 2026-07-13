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
    private const int MaxActiveAffiliates = 500;

    [Inject] private IAffiliateService AffiliateService { get; set; } = default!;
    [Inject] private IAdminBadgeService BadgeService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AdminAffiliatesTabCoordinator AffiliatesTabCoordinator { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "tab")]
    public string? TabQuery { get; set; }

    private string activeTab = "pending";
    private string pendingFilter = "pending";

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

    private bool showViewModal;
    private AffiliateApplicationModel? selectedApplication;
    private bool isLoading = true;
    private bool isActionInProgress;
    private string? processingPayoutCode;
    private string? errorMessage;
    private string? loadedCacheKey;

    private AffiliatePendingPayoutModel? payoutToConfirm;
    private string? expandedPayoutCode;
    private List<AffiliatePendingReferralModel> expandedReferrals = new();
    private bool isLoadingReferrals;

    private static readonly (string Tab, string Label)[] TabViews =
    {
        ("pending", "Pending Applications"),
        ("active", "Active Affiliates"),
        ("payouts", "Payouts"),
        ("tiers", "Tier Settings")
    };

    protected override async Task OnInitializedAsync()
    {
        AffiliatesTabCoordinator.TabChanged += OnAffiliatesTabChanged;
        NavigationManager.LocationChanged += OnLocationChanged;
        ApplyTabFromQuery();
        await LoadTabDataAsync(force: true);
    }

    protected override async Task OnParametersSetAsync()
    {
        ApplyTabFromQuery();
        await LoadTabDataAsync();
    }

    public void Dispose()
    {
        AffiliatesTabCoordinator.TabChanged -= OnAffiliatesTabChanged;
        NavigationManager.LocationChanged -= OnLocationChanged;
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
            await LoadTabDataAsync(force: true);
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
            loadedCacheKey = null;
            await LoadTabDataAsync(force: true);
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

    private void SetPendingFilter(string filter)
    {
        pendingFilter = filter;
    }

    private void SetPayoutView(string view)
    {
        if (payoutView == view)
            return;

        payoutView = view;
        if (view == "history")
            payoutHistoryPage = 1;

        loadedCacheKey = null;
        _ = LoadTabDataAsync(force: true);
    }

    private string GetTabCacheKey() =>
        activeTab == "payouts"
            ? $"{activeTab}:{payoutView}:{payoutHistoryPage}"
            : activeTab;

    private IEnumerable<AffiliateApplicationModel> FilteredPendingApplications =>
        pendingFilter == "waitlisted"
            ? waitListApplications
            : pendingApplications;

    private async Task LoadTabDataAsync(bool force = false)
    {
        if (!force && loadedCacheKey == GetTabCacheKey())
            return;

        isLoading = true;
        errorMessage = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            await LoadBadgeCountsAsync();

            if (activeTab is "pending" or "active")
                await LoadApplicationsAsync();
            else if (activeTab == "payouts")
                await LoadPayoutsAsync();
            else if (activeTab == "tiers")
                await LoadTiersAsync();
        }
        catch (Exception ex)
        {
            errorMessage = "Failed to load affiliate data.";
            Console.WriteLine($"[AdminAffiliates] Load error: {ex.Message}");
        }
        finally
        {
            isLoading = false;
            loadedCacheKey = GetTabCacheKey();
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadBadgeCountsAsync()
    {
        var result = await BadgeService.GetCounts();
        if (result.Success && result.Data != null)
            affiliateCounts = result.Data.AffiliateCounts;
    }

    private async Task LoadApplicationsAsync()
    {
        var pendingTask = AffiliateService.GetAllApplications("pending");
        var activeTask = AffiliateService.GetAllApplications("approved");
        var waitListTask = AffiliateService.GetAllApplications("waitlisted");

        await Task.WhenAll(pendingTask, activeTask, waitListTask);

        var pendingResult = await pendingTask;
        if (pendingResult.Success && pendingResult.Data != null)
            pendingApplications = pendingResult.Data;

        var activeResult = await activeTask;
        if (activeResult.Success && activeResult.Data != null)
            activeApplications = activeResult.Data;

        var waitlistResult = await waitListTask;
        if (waitlistResult.Success && waitlistResult.Data != null)
            waitListApplications = waitlistResult.Data;
    }

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
            return;
        }

        pendingPayouts = result.Data;
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
                loadedCacheKey = null;
                await LoadTabDataAsync(force: true);
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

    private static string FormatTierLabel(string tier)
    {
        if (string.IsNullOrWhiteSpace(tier) || tier.Equals("none", StringComparison.OrdinalIgnoreCase))
            return "Bronze";

        return char.ToUpperInvariant(tier[0]) + tier[1..].ToLowerInvariant();
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

        affiliateTiers = result.Data.OrderBy(t => t.SortOrder).ToList();
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

    private static (string Bg, string Fg) GetTierBadgeColors(string slug) =>
        slug.ToLowerInvariant() switch
        {
            "silver" => ("#E5E7EB", "#374151"),
            "gold" => ("#FEF3C7", "#92400E"),
            _ => ("#FEE2E2", "#991B1B")
        };

    private sealed class TierEditForm
    {
        public int ItemsSoldThreshold { get; set; }
        public decimal CommissionRatePercent { get; set; }
        public decimal ReferralDiscountPercent { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSaving { get; set; }
        public string? Error { get; set; }
        public string? Success { get; set; }

        public static TierEditForm FromTier(AffiliateTierModel tier) => new()
        {
            ItemsSoldThreshold = tier.ItemsSoldThreshold,
            CommissionRatePercent = tier.CommissionRatePercent,
            ReferralDiscountPercent = tier.ReferralDiscountPercent,
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
                loadedCacheKey = null;
                await LoadTabDataAsync(force: true);
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
        selectedApplication = pendingApplications.FirstOrDefault(a => a.Id == applicationId)
            ?? activeApplications.FirstOrDefault(a => a.Id == applicationId)
            ?? waitListApplications.FirstOrDefault(a => a.Id == applicationId);

        if (selectedApplication != null)
            showViewModal = true;
    }

    private void CloseViewModal()
    {
        showViewModal = false;
        selectedApplication = null;
    }

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
