using MuuqWear.Application.Shared;
using MuuqWear.Model.AffiliateApplication;
using MuuqWear.Model.PartnerStoreProduct;
using MuuqWear.Model.Shared;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.AffiliateService;


public class AffiliateService : IAffiliateService
{
    private readonly HttpClient _httpClient;

    public AffiliateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // =============================================
    // USER METHODS
    // =============================================

    public async Task<Response<AffiliateApplicationModel>> SubmitApplication(
        SubmitAffiliateApplicationModel request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/Affiliate/apply", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<Response<AffiliateApplicationModel>>();
                return error ?? Response<AffiliateApplicationModel>.Fail("Failed to submit application");
            }

            var result = await response.Content.ReadFromJsonAsync<Response<AffiliateApplicationModel>>();
            return result ?? Response<AffiliateApplicationModel>.Fail("Invalid response");
        }
        catch (Exception ex)
        {
            return Response<AffiliateApplicationModel>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<Response<AffiliateStatusModel>> GetStatus()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<Response<AffiliateStatusModel>>(
                "api/Affiliate/status");

            return result ?? Response<AffiliateStatusModel>.Fail("Failed to fetch status");
        }
        catch (Exception ex)
        {
            return Response<AffiliateStatusModel>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<Response<AffiliateApplicationModel?>> GetMyApplication()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<Response<AffiliateApplicationModel?>>(
                "api/Affiliate/application");

            return result ?? Response<AffiliateApplicationModel?>.Fail("Failed to fetch application");
        }
        catch (Exception ex)
        {
            return Response<AffiliateApplicationModel?>.Fail($"Error: {ex.Message}");
        }
    }

    // =============================================
    // ADMIN METHODS
    // =============================================

    public async Task<Response<List<AffiliateApplicationModel>>> GetAllApplications(
        string? statusFilter = null)
    {
        try
        {
            var url = string.IsNullOrEmpty(statusFilter)
                ? "api/Affiliate/admin/applications"
                : $"api/Affiliate/admin/applications?status={statusFilter}";

            var result = await _httpClient.GetFromJsonAsync<Response<List<AffiliateApplicationModel>>>(url);

            return result ?? Response<List<AffiliateApplicationModel>>.Fail("Failed to fetch applications");
        }
        catch (Exception ex)
        {
            return Response<List<AffiliateApplicationModel>>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<Response<AffiliateApplicationModel>> UpdateApplicationStatus(
        Guid applicationId, UpdateAffiliateApplicationStatusModel request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"api/Affiliate/admin/applications/{applicationId}/status", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<Response<AffiliateApplicationModel>>();
                return error ?? Response<AffiliateApplicationModel>.Fail("Failed to update status");
            }

            var result = await response.Content.ReadFromJsonAsync<Response<AffiliateApplicationModel>>();
            return result ?? Response<AffiliateApplicationModel>.Fail("Invalid response");
        }
        catch (Exception ex)
        {
            return Response<AffiliateApplicationModel>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<Response<bool>> ApproveApplication(Guid applicationId)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
     $"api/Affiliate/admin/approve/{applicationId}",
     new { });

            if (!response.IsSuccessStatusCode)
            {
                return Response<bool>.Fail("Failed to validate purchase");
            }

            var result = await response.Content.ReadFromJsonAsync<Response<bool>>();
            return result ?? Response<bool>.Fail("Invalid response");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Frontend] CanPurchaseQuantity error: {ex.Message}");
            return Response<bool>.Fail($"Error validating purchase: {ex.Message}");
        }
    }


    /// End Admin Methods////
    public async Task<Response<int>> GetPendingCount()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<Response<int>>(
                "api/Affiliate/spots-remaining");

            return result ?? Response<int>.Fail("Failed to fetch count");
        }
        catch (Exception ex)
        {
            return Response<int>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<Response<int>> GetSpotsRemaining()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<Response<int>>(
                "api/Affiliate/");

            return result ?? Response<int>.Fail("Failed to fetch count");
        }
        catch (Exception ex)
        {
            return Response<int>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<Response<AffiliateInfoModel>> GetAffiliateInfo()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/Affiliate/info");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return Response<AffiliateInfoModel>.Fail($"Error: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<Response<AffiliateInfoModel>>();
            return result ?? Response<AffiliateInfoModel>.Fail("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            return Response<AffiliateInfoModel>.Fail($"Error: {ex.Message}");
        }
    }
    public async Task<Response<bool>> ValidateAffiliateCode(string affiliateCode)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<Response<bool>>(
                $"api/Affiliate/validate/{affiliateCode}");

            return result ?? Response<bool>.Fail("Failed to validate code");
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail($"Error: {ex.Message}");
        }
    }



    public async Task<Response<bool>> TrackClick(TrackClickRequestModel request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/Affiliate/track-click", request);

            if (!response.IsSuccessStatusCode)
            {
                return Response<bool>.Fail("Failed to track click");
            }

            var result = await response.Content.ReadFromJsonAsync<Response<bool>>();
            return result ?? Response<bool>.Fail("Invalid response");
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<Response<bool>> HasRecentClick(string affiliateCode, string ipAddress)
    {
        try
        {
            var encodedIp = Uri.EscapeDataString(ipAddress);
            var result = await _httpClient.GetFromJsonAsync<Response<bool>>(
                $"api/Affiliate/check-recent-click/{affiliateCode}/{encodedIp}");

            return result ?? Response<bool>.Fail("Failed to check recent click");
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get performance chart data (last 30 days)
    /// </summary>
    public async Task<Response<PerformanceChartModel>> GetPerformanceChart()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/Affiliate/performance-chart");

            if (!response.IsSuccessStatusCode)
            {
                return Response<PerformanceChartModel>.Fail("Failed to load chart data");
            }

            var result = await response.Content.ReadFromJsonAsync<Response<PerformanceChartModel>>();
            return result ?? Response<PerformanceChartModel>.Fail("Invalid response");
        }
        catch (Exception ex)
        {
            return Response<PerformanceChartModel>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<Response<PaginatedResponse<PartnerStoreProductModel>>> GetPartnerStoreProducts(
     int page = 1,
     int pageSize = 10)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/Affiliate/partner-store/products?page={page}&pageSize={pageSize}");

            if (!response.IsSuccessStatusCode)
            {
                return Response<PaginatedResponse<PartnerStoreProductModel>>.Fail(
                    "Failed to load partner store products");
            }

            var result = await response.Content
                .ReadFromJsonAsync<Response<PaginatedResponse<PartnerStoreProductModel>>>();

            return result ?? Response<PaginatedResponse<PartnerStoreProductModel>>.Fail("Invalid response");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [Frontend] GetPartnerStoreProducts error: {ex.Message}");
            return Response<PaginatedResponse<PartnerStoreProductModel>>.Fail(
                $"Error loading products: {ex.Message}");
        }
    }

    public async Task<Response<AffiliatePurchaseLimitModel>> GetPurchaseLimitStatus()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/Affiliate/partner-store/purchase-limit");

            if (!response.IsSuccessStatusCode)
            {
                return Response<AffiliatePurchaseLimitModel>.Fail(
                    "Failed to load purchase limit");
            }

            var result = await response.Content.ReadFromJsonAsync<Response<AffiliatePurchaseLimitModel>>();
            return result ?? Response<AffiliatePurchaseLimitModel>.Fail("Invalid response");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [Frontend] GetPurchaseLimitStatus error: {ex.Message}");
            return Response<AffiliatePurchaseLimitModel>.Fail(
                $"Error loading limit: {ex.Message}");
        }
    }

    public async Task<Response<bool>> CanPurchaseQuantity(int quantity)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/Affiliate/partner-store/can-purchase/{quantity}");

            if (!response.IsSuccessStatusCode)
            {
                return Response<bool>.Fail("Failed to validate purchase");
            }

            var result = await response.Content.ReadFromJsonAsync<Response<bool>>();
            return result ?? Response<bool>.Fail("Invalid response");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Frontend] CanPurchaseQuantity error: {ex.Message}");
            return Response<bool>.Fail($"Error validating purchase: {ex.Message}");
        }
    }
    public async Task<Response<List<RecentReferralModel>>> GetRecentReferrals()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/Affiliate/recent-referrals");

            if (!response.IsSuccessStatusCode)
            {
                return Response<List<RecentReferralModel>>.Fail(
                    "Failed to load recent referrals");
            }

            var result = await response.Content
                .ReadFromJsonAsync<Response<List<RecentReferralModel>>>();

            return result ?? Response<List<RecentReferralModel>>.Fail("Invalid response");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [Frontend] GetRecentReferrals error: {ex.Message}");
            return Response<List<RecentReferralModel>>.Fail(
                $"Error loading referrals: {ex.Message}");
        }
    }



}

