using MuuqWear.Application.Services.AffiliateService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.AffiliateApplication;
using MuuqWear.Web.ServerAuth;
using MuuqWear.Model.PartnerStoreProduct;
using MuuqWear.Model.Shared;

namespace MuuqWear.Web.Services;

public sealed class FirestoreAffiliateService : IAffiliateService
{
    private readonly AdminFirestore _store;
    private readonly IHttpContextAccessor _http;
    private static readonly List<AffiliateTierModel> DefaultTiers =
    [
        new() { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), Slug = "starter", DisplayName = "Starter", CommissionRatePercent = 10, ReferralDiscountPercent = 5, IsActive = true, SortOrder = 1 },
        new() { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), Slug = "partner", DisplayName = "Partner", ItemsSoldThreshold = 10, CommissionRatePercent = 12, ReferralDiscountPercent = 8, IsActive = true, SortOrder = 2 }
    ];

    public FirestoreAffiliateService(AdminFirestore store, IHttpContextAccessor http)
    {
        _store = store;
        _http = http;
    }

    public async Task<Response<AffiliateApplicationModel>> SubmitApplication(SubmitAffiliateApplicationModel request)
    {
        request.Email ??= CurrentEmail();
        return Response<AffiliateApplicationModel>.SuccessResponse(await _store.SubmitAffiliateAsync(request));
    }

    public async Task<Response<AffiliateStatusModel>> GetStatus()
    {
        var mine = await FindMineAsync();
        return Response<AffiliateStatusModel>.SuccessResponse(new AffiliateStatusModel
        {
            ApplicationStatus = mine?.Status ?? "not_applied",
            SubmittedAt = mine?.SubmittedAt
        });
    }

    public async Task<Response<AffiliateApplicationModel?>> GetMyApplication() =>
        Response<AffiliateApplicationModel?>.SuccessResponse(await FindMineAsync());

    public async Task<Response<List<AffiliateApplicationModel>>> GetAllApplications(string? statusFilter = null) =>
        Response<List<AffiliateApplicationModel>>.SuccessResponse(
            await _store.ListAffiliateApplicationsAsync(statusFilter));

    public async Task<Response<AffiliateApplicationModel>> UpdateApplicationStatus(
        Guid applicationId, UpdateAffiliateApplicationStatusModel request)
    {
        var updated = await _store.UpdateAffiliateStatusAsync(applicationId, request.Status, request.AdminNotes);
        return updated is null
            ? Response<AffiliateApplicationModel>.Fail("Application not found")
            : Response<AffiliateApplicationModel>.SuccessResponse(updated);
    }

    public async Task<Response<int>> GetPendingCount() =>
        Response<int>.SuccessResponse((await _store.ListAffiliateApplicationsAsync("pending")).Count);

    public Task<Response<int>> GetSpotsRemaining() =>
        Task.FromResult(Response<int>.SuccessResponse(50));

    public Task<Response<AffiliateInfoModel>> GetAffiliateInfo() =>
        Task.FromResult(Response<AffiliateInfoModel>.SuccessResponse(new AffiliateInfoModel()));

    public async Task<Response<bool>> ValidateAffiliateCode(string affiliateCode)
    {
        var ok = await _store.IsActiveReferralCodeAsync(affiliateCode);
        return Response<bool>.SuccessResponse(ok);
    }

    public Task<Response<bool>> TrackClick(TrackClickRequestModel request) =>
        Task.FromResult(Response<bool>.SuccessResponse(true));

    public Task<Response<bool>> HasRecentClick(string affiliateCode, string ipAddress) =>
        Task.FromResult(Response<bool>.SuccessResponse(false));

    public Task<Response<PerformanceChartModel>> GetPerformanceChart() =>
        Task.FromResult(Response<PerformanceChartModel>.SuccessResponse(new PerformanceChartModel()));

    public Task<Response<PaginatedResponse<PartnerStoreProductModel>>> GetPartnerStoreProducts(
        int page = 1, int pageSize = 15) =>
        Task.FromResult(Response<PaginatedResponse<PartnerStoreProductModel>>.SuccessResponse(
            PaginatedResponse<PartnerStoreProductModel>.Empty(page, pageSize)));

    public Task<Response<AffiliatePurchaseLimitModel>> GetPurchaseLimitStatus() =>
        Task.FromResult(Response<AffiliatePurchaseLimitModel>.SuccessResponse(new AffiliatePurchaseLimitModel
        {
            ItemsRemaining = 20,
            MonthlyLimit = 20,
            MonthStartDate = DateTime.UtcNow.Date,
            NextResetDate = DateTime.UtcNow.Date.AddMonths(1)
        }));

    public Task<Response<bool>> CanPurchaseQuantity(int quantity) =>
        Task.FromResult(Response<bool>.SuccessResponse(true));

    public Task<Response<List<RecentReferralModel>>> GetRecentReferrals() =>
        Task.FromResult(Response<List<RecentReferralModel>>.SuccessResponse([]));

    public async Task<Response<bool>> ApproveApplication(Guid applicationId)
    {
        var result = await UpdateApplicationStatus(applicationId, new UpdateAffiliateApplicationStatusModel { Status = "approved" });
        return result.Success
            ? Response<bool>.SuccessResponse(true)
            : Response<bool>.Fail(result.Message);
    }

    public Task<Response<List<AffiliateTierModel>>> GetAdminTiers() =>
        Task.FromResult(Response<List<AffiliateTierModel>>.SuccessResponse(DefaultTiers));

    public Task<Response<AffiliateTierModel>> GetAdminTier(string slug)
    {
        var tier = DefaultTiers.FirstOrDefault(t => t.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(tier is null
            ? Response<AffiliateTierModel>.Fail("Tier not found")
            : Response<AffiliateTierModel>.SuccessResponse(tier));
    }

    public Task<Response<AffiliateTierModel>> UpdateAdminTier(string slug, UpdateAffiliateTierModel request)
    {
        var tier = DefaultTiers.FirstOrDefault(t => t.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        if (tier is null)
            return Task.FromResult(Response<AffiliateTierModel>.Fail("Tier not found"));
        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            tier.DisplayName = request.DisplayName;
        if (request.CommissionRatePercent is decimal rate)
            tier.CommissionRatePercent = rate;
        if (request.IsActive is bool active)
            tier.IsActive = active;
        return Task.FromResult(Response<AffiliateTierModel>.SuccessResponse(tier));
    }

    public Task<Response<List<AffiliateTierModel>>> GetTiers() => GetAdminTiers();

    public Task<Response<List<AffiliatePendingPayoutModel>>> GetAdminPendingPayouts() =>
        Task.FromResult(Response<List<AffiliatePendingPayoutModel>>.SuccessResponse([]));

    public Task<Response<List<AffiliatePendingReferralModel>>> GetAdminPendingReferrals(string affiliateCode) =>
        Task.FromResult(Response<List<AffiliatePendingReferralModel>>.SuccessResponse([]));

    public Task<Response<AffiliatePayoutResultModel>> ProcessAdminPayout(
        string affiliateCode, ProcessAffiliatePayoutModel? request = null) =>
        Task.FromResult(Response<AffiliatePayoutResultModel>.Fail("No pending payouts."));

    public Task<Response<PaginatedResponse<AffiliatePayoutResultModel>>> GetAdminPayoutHistory(
        int page = 1, int pageSize = 20) =>
        Task.FromResult(Response<PaginatedResponse<AffiliatePayoutResultModel>>.SuccessResponse(
            PaginatedResponse<AffiliatePayoutResultModel>.Empty(page, pageSize)));

    public async Task<Response<AffiliateAdminStatsModel>> GetAdminStats()
    {
        var all = await _store.ListAffiliateApplicationsAsync(null);
        var pending = all.Count(a => a.Status.Equals("pending", StringComparison.OrdinalIgnoreCase));
        var waitlisted = all.Count(a => a.Status.Equals("waitlisted", StringComparison.OrdinalIgnoreCase));
        var approved = all.Count(a => a.Status.Equals("approved", StringComparison.OrdinalIgnoreCase));
        return Response<AffiliateAdminStatsModel>.SuccessResponse(new AffiliateAdminStatsModel
        {
            PendingApplications = pending,
            WaitlistedApplications = waitlisted,
            ActiveAffiliates = approved,
            ApplicationsThisMonth = all.Count(a => a.SubmittedAt.Month == DateTime.UtcNow.Month && a.SubmittedAt.Year == DateTime.UtcNow.Year)
        });
    }

    public Task<Response<AffiliateApplicationModel>> UpdateAffiliateActiveStatus(
        Guid userId, UpdateAffiliateActiveStatusModel request) =>
        Task.FromResult(Response<AffiliateApplicationModel>.Fail("Affiliate not found"));

    public Task<Response<ProcessAllAffiliatePayoutsResultModel>> ProcessAllAdminPayouts(
        ProcessAllAffiliatePayoutsModel? request = null) =>
        Task.FromResult(Response<ProcessAllAffiliatePayoutsResultModel>.SuccessResponse(new ProcessAllAffiliatePayoutsResultModel()));

    private async Task<AffiliateApplicationModel?> FindMineAsync()
    {
        var email = CurrentEmail();
        if (string.IsNullOrWhiteSpace(email))
            return null;
        return (await _store.ListAffiliateApplicationsAsync(null))
            .FirstOrDefault(a => a.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    private string? CurrentEmail()
    {
        var (_, email, _) = VerifiedAuthSession.Shopper(_http.HttpContext?.User);
        return email;
    }
}
