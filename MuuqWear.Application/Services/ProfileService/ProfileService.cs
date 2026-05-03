using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Profile;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.ProfileService;

public class ProfileService : IProfileService
{
    private readonly HttpClient _http;

    public ProfileService(HttpClient http)
    {
        _http = http;
    }

    // =============================================
    // GET PROFILE
    // =============================================
    public async Task<Response<ProfileModel>> GetProfile()
    {
        try
        {
            var result = await _http.GetAsync("api/Profile");
            return await ReadResponse<ProfileModel>(result);
        }
        catch (Exception ex)
        {
            return Response<ProfileModel>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // UPDATE PROFILE
    // =============================================
    public async Task<Response<ProfileModel>> UpdateProfile(UpdateProfileModel request)
    {
        try
        {
            var result = await _http.PutAsJsonAsync("api/Profile", request);
            return await ReadResponse<ProfileModel>(result);
        }
        catch (Exception ex)
        {
            return Response<ProfileModel>.Fail("Error: " + ex.Message);
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

    // =============================================
    // DELETE ACCOUNT
    // =============================================
    public async Task<Response<bool>> DeleteAccount()
    {
        try
        {
            var result = await _http.DeleteAsync("api/Profile/delete-account");
            return await ReadResponse<bool>(result);
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail("Error: " + ex.Message);
        }
    }
}