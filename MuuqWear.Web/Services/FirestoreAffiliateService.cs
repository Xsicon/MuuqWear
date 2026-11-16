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
    private readonly IConfiguration _config;
    public FirestoreAffiliateService(AdminFirestore store, IHttpContextAccessor http, IConfiguration config)
    {
        _store = store;
        _http = http;
        _config = config;
    }

    public async Task<Response<AffiliateApplicationModel>> SubmitApplication(SubmitAffiliateApplicationModel request)
    {
        request.Email ??= CurrentEmail();
        return Response<AffiliateApplicationModel>.SuccessResponse(await _store.SubmitAffiliateAsync(request));
    }

    public Task<Response<string>> UploadSampleFile(string fileName, byte[] bytes, string contentType) =>
        FirebaseStorageImages.UploadAsync(
            _config,
            "affiliate-samples",
            fileName,
            bytes,
            contentType,
            FirebaseStorageImages.AllowAffiliateSample);

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

    public async Task<Response<List<AffiliateTierModel>>> GetAdminTiers() =>
        Response<List<AffiliateTierModel>>.SuccessResponse(
            AffiliateTierCatalog.FilterCanonical(await _store.ListAffiliateTiersAsync()));

    public async Task<Response<AffiliateTierModel>> GetAdminTier(string slug)
    {
        var tier = await _store.GetAffiliateTierAsync(slug)
            ?? AffiliateTierCatalog.DefaultTiers.FirstOrDefault(t =>
                t.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        return tier is null
            ? Response<AffiliateTierModel>.Fail("Tier not found")
            : Response<AffiliateTierModel>.SuccessResponse(tier);
    }

    public async Task<Response<AffiliateTierModel>> UpdateAdminTier(string slug, UpdateAffiliateTierModel request)
    {
        var existing = await _store.GetAffiliateTierAsync(slug)
            ?? AffiliateTierCatalog.DefaultTiers.FirstOrDefault(t =>
                t.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase))
            ?? new AffiliateTierModel { Slug = slug };

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            existing.DisplayName = request.DisplayName!;
        if (request.ItemsSoldThreshold is int threshold)
            existing.ItemsSoldThreshold = threshold;
        if (request.CommissionRatePercent is decimal rate)
            existing.CommissionRatePercent = rate;
        if (request.ReferralDiscountPercent is decimal discount)
            existing.ReferralDiscountPercent = discount;
        if (request.QuarterlyBonusPercent is decimal bonus)
            existing.QuarterlyBonusPercent = bonus;
        if (request.MaxAffiliates is int max)
            existing.MaxAffiliates = max;
        if (request.Perks is not null)
            existing.Perks = request.Perks;
        if (request.SortOrder is int sort)
            existing.SortOrder = sort;
        if (request.IsActive is bool active)
            existing.IsActive = active;

        return Response<AffiliateTierModel>.SuccessResponse(await _store.UpsertAffiliateTierAsync(existing));
    }

    public Task<Response<List<AffiliateTierModel>>> GetTiers() => GetAdminTiers();

    public async Task<Response<List<AffiliatePendingPayoutModel>>> GetAdminPendingPayouts()
    {
        var pending = await EnrichCommissionsAsync(await _store.ListPendingCommissionsAsync());
        var groups = pending
            .GroupBy(c => string.IsNullOrWhiteSpace(c.AmbassadorId) ? "unknown" : c.AmbassadorId)
            .Select(g => new AffiliatePendingPayoutModel
            {
                AffiliateCode = g.Key,
                AffiliateName = g.First().AmbassadorName,
                AffiliateTier = "bronze",
                TotalAmount = g.Sum(c => c.AmountCents) / 100m,
                ReferralCount = g.Count(),
                OldestPendingDate = g.Min(c => c.CreatedAt),
                Status = "pending",
                PaymentMethod = "manual"
            })
            .OrderByDescending(p => p.TotalAmount)
            .ToList();
        return Response<List<AffiliatePendingPayoutModel>>.SuccessResponse(groups);
    }

    public async Task<Response<List<AffiliatePendingReferralModel>>> GetAdminPendingReferrals(string affiliateCode)
    {
        var pending = await EnrichCommissionsAsync(await _store.ListPendingCommissionsAsync());
        var rows = pending
            .Where(c => c.AmbassadorId.Equals(affiliateCode, StringComparison.OrdinalIgnoreCase))
            .Select(c => new AffiliatePendingReferralModel
            {
                Id = Guid.TryParse(c.Id, out var id) ? id : Guid.NewGuid(),
                OrderId = c.OrderId,
                OrderNumber = c.OrderId == Guid.Empty ? c.Id[..Math.Min(8, c.Id.Length)].ToUpperInvariant() : c.OrderId.ToString("N")[..8].ToUpperInvariant(),
                OrderTotal = c.OrderTotalCents / 100m,
                CommissionAmount = c.AmountCents / 100m,
                CommissionRate = (int)Math.Round(c.Rate * 100),
                CreatedAt = c.CreatedAt,
                MaskedCustomer = "Customer"
            })
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
        return Response<List<AffiliatePendingReferralModel>>.SuccessResponse(rows);
    }

    public async Task<Response<AffiliatePayoutResultModel>> ProcessAdminPayout(
        string affiliateCode, ProcessAffiliatePayoutModel? request = null)
    {
        var pending = await EnrichCommissionsAsync(await _store.ListPendingCommissionsAsync());
        var rows = pending
            .Where(c => c.AmbassadorId.Equals(affiliateCode, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (rows.Count == 0)
            return Response<AffiliatePayoutResultModel>.Fail("No pending payouts.");

        var result = await _store.ProcessAffiliatePayoutAsync(
            affiliateCode, rows, request?.PaymentMethod, request?.AdminNotes);
        await _store.WriteSystemLogAsync("info",
            $"Processed payout {result.FormattedAmount} for {affiliateCode}", "affiliates");
        return Response<AffiliatePayoutResultModel>.SuccessResponse(result);
    }

    public async Task<Response<PaginatedResponse<AffiliatePayoutResultModel>>> GetAdminPayoutHistory(
        int page = 1, int pageSize = 20)
    {
        var all = await _store.ListAffiliatePayoutsAsync();
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        return Response<PaginatedResponse<AffiliatePayoutResultModel>>.SuccessResponse(new PaginatedResponse<AffiliatePayoutResultModel>
        {
            Data = all.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = all.Count
        });
    }

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

    public async Task<Response<AffiliateApplicationModel>> UpdateAffiliateActiveStatus(
        Guid idOrUserId, UpdateAffiliateActiveStatusModel request)
    {
        var updated = await _store.UpdateAffiliateActiveAsync(idOrUserId, request.IsActive);
        return updated is null
            ? Response<AffiliateApplicationModel>.Fail("Affiliate not found")
            : Response<AffiliateApplicationModel>.SuccessResponse(updated);
    }

    public async Task<Response<ProcessAllAffiliatePayoutsResultModel>> ProcessAllAdminPayouts(
        ProcessAllAffiliatePayoutsModel? request = null)
    {
        var pending = await GetAdminPendingPayouts();
        var groups = pending.Data ?? [];
        var result = new ProcessAllAffiliatePayoutsResultModel();
        foreach (var group in groups)
        {
            var payout = await ProcessAdminPayout(group.AffiliateCode, new ProcessAffiliatePayoutModel
            {
                PaymentMethod = request?.PaymentMethod,
                AdminNotes = request?.AdminNotes
            });
            if (!payout.Success || payout.Data is null)
                continue;
            result.ProcessedCount++;
            result.TotalAmount += payout.Data.TotalAmount;
            result.PayoutIds.Add(payout.Data.PayoutId);
        }

        return Response<ProcessAllAffiliatePayoutsResultModel>.SuccessResponse(result);
    }

    private async Task<List<AdminFirestore.CommissionRow>> EnrichCommissionsAsync(
        List<AdminFirestore.CommissionRow> rows)
    {
        var apps = await _store.ListAffiliateApplicationsAsync(null);
        foreach (var row in rows)
        {
            var app = apps.FirstOrDefault(a =>
                a.Id.ToString().Equals(row.AmbassadorId, StringComparison.OrdinalIgnoreCase)
                || a.Email.Equals(row.AmbassadorId, StringComparison.OrdinalIgnoreCase));
            var profileName = await _store.GetCustomerDisplayNameAsync(row.AmbassadorId);
            row.AmbassadorName = !string.IsNullOrWhiteSpace(app?.FullName)
                ? app!.FullName
                : (!string.IsNullOrWhiteSpace(profileName) ? profileName! : row.AmbassadorId);
        }

        return rows;
    }

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
