using MuuqWear.Application.Shared;
using MuuqWear.Model.AffiliateApplication;
using MuuqWear.Model.PartnerStoreProduct;
using MuuqWear.Model.Shared;

namespace MuuqWear.Application.Services.AffiliateService;

public interface IAffiliateService
{
    Task<Response<AffiliateApplicationModel>> SubmitApplication(SubmitAffiliateApplicationModel request);
    Task<Response<AffiliateStatusModel>> GetStatus();
    Task<Response<AffiliateApplicationModel?>> GetMyApplication();

    // Admin
    Task<Response<List<AffiliateApplicationModel>>> GetAllApplications(string? statusFilter = null);
    Task<Response<AffiliateApplicationModel>> UpdateApplicationStatus(Guid applicationId, UpdateAffiliateApplicationStatusModel request);
    Task<Response<int>> GetPendingCount();
    Task<Response<int>> GetSpotsRemaining();
    Task<Response<AffiliateInfoModel>> GetAffiliateInfo();
    Task<Response<bool>> ValidateAffiliateCode(string affiliateCode);
    Task<Response<bool>> TrackClick(TrackClickRequestModel request);
    Task<Response<bool>> HasRecentClick(string affiliateCode, string ipAddress);  //  ADD THIS
    Task<Response<PerformanceChartModel>> GetPerformanceChart();
    Task<Response<PaginatedResponse<PartnerStoreProductModel>>> GetPartnerStoreProducts(
        int page = 1,
        int pageSize = 15);
    Task<Response<AffiliatePurchaseLimitModel>> GetPurchaseLimitStatus();

    Task<Response<bool>> CanPurchaseQuantity(int quantity);
    Task<Response<List<RecentReferralModel>>> GetRecentReferrals();
    Task<Response<bool>> ApproveApplication(Guid applicationId);

}

