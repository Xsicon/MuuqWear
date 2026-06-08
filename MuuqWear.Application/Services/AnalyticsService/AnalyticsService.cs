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
            return await ReadResponse<RevenueOverTimeModel>(result);
        }
        catch (Exception ex)
        {
            return Response<RevenueOverTimeModel>.Fail("Error: " + ex.Message);
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
            return await ReadResponse<List<TopSellingProductModel>>(result);
        }
        catch (Exception ex)
        {
            return Response<List<TopSellingProductModel>>.Fail("Error: " + ex.Message);
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
            return await ReadResponse<List<AffiliatePerformanceModel>>(result);
        }
        catch (Exception ex)
        {
            return Response<List<AffiliatePerformanceModel>>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // HELPER
    // =============================================
    private async Task<Response<T>> ReadResponse<T>(HttpResponseMessage response)
    {
        try
        {
            var result = await response.Content
                .ReadFromJsonAsync<Response<T>>();
            return result ?? Response<T>.Fail("Empty response");
        }
        catch
        {
            return Response<T>.Fail("Failed to parse response");
        }
    }
}
