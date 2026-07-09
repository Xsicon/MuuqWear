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
            return await HttpResponseReader.ReadAsync<ProfileModel>(result);
        }
        catch (Exception ex)
        {
            return Response<ProfileModel>.Fail(HttpResponseReader.FromException(ex));
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
            return await HttpResponseReader.ReadAsync<ProfileModel>(result);
        }
        catch (Exception ex)
        {
            return Response<ProfileModel>.Fail(HttpResponseReader.FromException(ex));
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
            return await HttpResponseReader.ReadAsync<bool>(result);
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail(HttpResponseReader.FromException(ex));
        }
    }
}