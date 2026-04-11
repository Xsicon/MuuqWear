using MuuqWear.Application.Shared;
using MuuqWear.Model.Authentication;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.AuthService;
public class AuthService : IAuthService
{
    private readonly HttpClient _http;

    public AuthService(HttpClient http)
    {
        _http = http;
    }
    public async Task<Response<int>> Register(RegisterModel request)
    {
        var result = await _http.PostAsJsonAsync("api/Auth/register", request);

        var response = await result.Content.ReadFromJsonAsync<Response<int>>();

        return response ?? new Response<int>
        {
            Success = false,
            Message = "Empty response from server"
        };
    }
    public async Task<Response<AuthResponseModel>> VerifyOTP(VerifyOTPModel request)
    {
        var result = await _http.PostAsJsonAsync("api/Auth/verifyotp", request);

        if (!result.IsSuccessStatusCode)
        {
            return new Response<AuthResponseModel>
            {
                Success = false,
                Message = $"Server error: {result.StatusCode}"
            };
        }

        var response = await result.Content.ReadFromJsonAsync<Response<AuthResponseModel>>();

        return response ?? new Response<AuthResponseModel>
        {
            Success = false,
            Message = "Empty response from server"
        };
    }
}
