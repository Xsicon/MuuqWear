using MuuqWear.Application.Shared;
using MuuqWear.Model.AdminSettingsUser;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.AdminUserService;

public class AdminSettingService : IAdminSettingService
{
    private readonly HttpClient _http;

    public AdminSettingService(HttpClient http)
    {
        _http = http;
    }

    // =============================================
    // GET ALL ADMIN USERS
    // =============================================
    public async Task<Response<List<AdminSettingsUserModel>>> GetAll()
    {
        try
        {
            var result = await _http.GetAsync("api/AdminSetting");

            var response = await result.Content
                .ReadFromJsonAsync<Response<List<AdminSettingsUserModel>>>();

            return response ?? new Response<List<AdminSettingsUserModel>>
            {
                Success = false,
                Message = "Unexpected response from server"
            };
        }
        catch (Exception)
        {
            return new Response<List<AdminSettingsUserModel>>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    // =============================================
    // INVITE ADMIN USER
    // =============================================
    public async Task<Response<AdminSettingsUserModel>> Invite(
        InviteAdminSettingsUserModel request)
    {
        try
        {
            var result = await _http.PostAsJsonAsync(
                "api/AdminSetting/invite", request);

            var response = await result.Content
                .ReadFromJsonAsync<Response<AdminSettingsUserModel>>();

            return response ?? new Response<AdminSettingsUserModel>
            {
                Success = false,
                Message = result.IsSuccessStatusCode
                    ? "Unexpected response from server"
                    : $"Server error: {result.StatusCode}"
            };
        }
        catch (Exception)
        {
            return new Response<AdminSettingsUserModel>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    // =============================================
    // UPDATE ADMIN USER
    // =============================================
    public async Task<Response<AdminSettingsUserModel>> Update(
        Guid userId, UpdateAdminSettingsUserModel request)
    {
        try
        {
            var result = await _http.PatchAsJsonAsync(
                $"api/AdminSetting/{userId}", request);

            var response = await result.Content
                .ReadFromJsonAsync<Response<AdminSettingsUserModel>>();

            return response ?? new Response<AdminSettingsUserModel>
            {
                Success = false,
                Message = result.IsSuccessStatusCode
                    ? "Unexpected response from server"
                    : $"Server error: {result.StatusCode}"
            };
        }
        catch (Exception)
        {
            return new Response<AdminSettingsUserModel>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    // =============================================
    // DEACTIVATE ADMIN USER
    // =============================================
    public async Task<Response<bool>> Deactivate(Guid userId)
    {
        try
        {
            var result = await _http.DeleteAsync(
                $"api/AdminSetting/{userId}");

            var response = await result.Content
                .ReadFromJsonAsync<Response<bool>>();

            return response ?? new Response<bool>
            {
                Success = false,
                Message = result.IsSuccessStatusCode
                    ? "Unexpected response from server"
                    : $"Server error: {result.StatusCode}"
            };
        }
        catch (Exception)
        {
            return new Response<bool>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    public async Task<Response<SupabaseHealthModel>> CheckSupabaseHealth()
    {
        try
        {
            var result = await _http.GetAsync(
                "api/AdminSetting/supabase-health");
            var response = await result.Content
                .ReadFromJsonAsync<Response<SupabaseHealthModel>>();
            return response ?? new Response<SupabaseHealthModel>
            {
                Success = false,
                Message = "Unexpected response from server"
            };
        }
        catch (Exception)
        {
            return new Response<SupabaseHealthModel>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    public async Task<Response<StripeHealthModel>> CheckStripeHealth()
    {
        try
        {
            var result = await _http.GetAsync(
                "api/AdminSetting/stripe-health");
            var response = await result.Content
                .ReadFromJsonAsync<Response<StripeHealthModel>>();
            return response ?? new Response<StripeHealthModel>
            {
                Success = false,
                Message = "Unexpected response from server"
            };
        }
        catch (Exception)
        {
            return new Response<StripeHealthModel>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }
}
