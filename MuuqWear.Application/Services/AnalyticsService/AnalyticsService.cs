using MuuqWear.Application.Shared;
using MuuqWear.Model.AffiliatePerfomanceModel;
using MuuqWear.Model.RevenueOverTime;
using MuuqWear.Model.TopSellingProduct;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.AnalyticsService;

public class AnalyticsService : IAnalyticsService
{
    private readonly HttpClient _http;

    public AnalyticsService(HttpClient http)
    {
        _http = http;
    }

    // =============================================
    // REVENUE OVER TIME
    // =============================================
    public async Task<Response<RevenueOverTimeModel>> GetRevenue()
    {
        try
        {
            var result = await _http.GetAsync("api/Analytics/revenue");
            return await HttpResponseReader.ReadAsync<RevenueOverTimeModel>(result);
        }
        catch (Exception ex)
        {
            return Response<RevenueOverTimeModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    // =============================================
    // TOP SELLING PRODUCTS
    // =============================================
    public async Task<Response<List<TopSellingProductModel>>> GetTopProducts(int limit = 5)
    {
        try
        {
            var result = await _http.GetAsync(
                $"api/Analytics/top-products?limit={limit}");
            return await HttpResponseReader.ReadAsync<List<TopSellingProductModel>>(result);
        }
        catch (Exception ex)
        {
            return Response<List<TopSellingProductModel>>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    // =============================================
    // AFFILIATE PERFORMANCE
    // =============================================
    public async Task<Response<List<AffiliatePerformanceModel>>> GetAffiliatePerformance()
    {
        try
        {
            var result = await _http.GetAsync("api/Analytics/affiliate-performance");
            return await HttpResponseReader.ReadAsync<List<AffiliatePerformanceModel>>(result);
        }
        catch (Exception ex)
        {
            return Response<List<AffiliatePerformanceModel>>.Fail(HttpResponseReader.FromException(ex));
        }
    }
}
