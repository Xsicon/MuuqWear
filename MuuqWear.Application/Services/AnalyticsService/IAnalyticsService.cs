using MuuqWear.Application.Shared;
using MuuqWear.Model.AffiliatePerfomanceModel;
using MuuqWear.Model.RevenueOverTime;
using MuuqWear.Model.TopSellingProduct;

namespace MuuqWear.Application.Services.AnalyticsService;

public interface IAnalyticsService
{
    Task<Response<RevenueOverTimeModel>> GetRevenue();
    Task<Response<List<TopSellingProductModel>>> GetTopProducts(int limit = 5);
    Task<Response<List<AffiliatePerformanceModel>>> GetAffiliatePerformance();
}
