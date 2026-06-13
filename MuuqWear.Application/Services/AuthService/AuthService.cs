using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Authentication;
using MuuqWear.Model.OrderReturn;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.AuthService;
public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly ProtectedSessionStorage _sessionStorage;
    public bool IsNewSignup { get; private set; } = false;


    public AuthService(HttpClient http, ProtectedSessionStorage sessionStorage)
    {
        this._http = http;
        this._sessionStorage = sessionStorage;
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


        var response = await result.Content.ReadFromJsonAsync<Response<AuthResponseModel>>();
        if (response?.Success == true && response.Data != null)
        {
            IsNewSignup = true; // User is now fully registered and logged in
        }
        return response ?? new Response<AuthResponseModel>
        {
            Success = false,
            Message = "Empty response from server"
        };
    }

    public async Task<bool> IsUserLoggedIn()
    {
        bool flag = false;
        var result = await _sessionStorage.GetAsync<string>("userKey");
        if (result.Success)
        {
            flag = true;
        }
        return flag;
    }

    public async Task<Response<AuthResponseModel>> Login(LoginModel request)
    {
        try
        {
            var result = await _http.PostAsJsonAsync("api/Auth/login", request);
            var response = await result.Content.ReadFromJsonAsync<Response<AuthResponseModel>>();
            if (response != null)
                return response;
            return new Response<AuthResponseModel>
            {
                Success = false,
                Message = result.IsSuccessStatusCode
                        ? "Unexpected response from server"
                        : $"Server error: {result.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new Response<AuthResponseModel>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }
    public async Task<Response<int>> Logout()
    {
        try
        {
            var result = await _http.PostAsync("api/Auth/logout", null);
            var response = await result.Content.ReadFromJsonAsync<Response<int>>();
            if (response != null)
                return response;

            return new Response<int>
            {
                Success = false,
                Message = result.IsSuccessStatusCode
                    ? "Unexpected response from server"
                    : $"Server error: {result.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new Response<int>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    public async Task<Response<int>> SendMagicLink(MagicLinkModel request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return new Response<int>
                {
                    Success = false,
                    Message = "Email is required"
                };

            var result = await _http.PostAsJsonAsync(
         "api/Auth/magic-link", request);


            var response = await result.Content
                .ReadFromJsonAsync<Response<int>>();

            if (response != null)
                return response;

            return new Response<int>
            {
                Success = false,
                Message = result.IsSuccessStatusCode
                    ? "Unexpected response from server"
                    : $"Server error: {result.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            //  network errors, timeouts, JSON parse failures
            return new Response<int>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    public async Task<Response<AuthResponseModel>> VerifyMagicLink(
    MagicLinkVerifyModel request)
    {
        try
        {
            var result = await _http.PostAsJsonAsync(
                "api/Auth/verify-magic-link", request);


            var response = await result.Content
                .ReadFromJsonAsync<Response<AuthResponseModel>>();

            if (response != null)
                return response;

            return new Response<AuthResponseModel>
            {
                Success = false,
                Message = result.IsSuccessStatusCode
                    ? "Unexpected response from server"
                    : $"Server error: {result.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new Response<AuthResponseModel>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    public async Task<Response<string>> GetGoogleSignInUrl()
    {
        try
        {
            var response = await _http
                .GetFromJsonAsync<Response<string>>("api/Auth/google-signin-url");

            return response ?? new Response<string>
            {
                Success = false,
                Message = "Unexpected response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<string>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    public async Task<Response<int>> SendPasswordReset(ForgotPasswordModel request)
    {
        //  validate before hitting API — no need to waste a network call
        if (string.IsNullOrWhiteSpace(request.Email))
            return new Response<int>
            {
                Success = false,
                Message = "Email is required"
            };

        try
        {
            var result = await _http.PostAsJsonAsync(
                "api/Auth/forgot-password", request);

            var response = await result.Content
                .ReadFromJsonAsync<Response<int>>();

            return response ?? new Response<int>
            {
                Success = false,
                Message = result.IsSuccessStatusCode
                    ? "Unexpected response from server"
                    : $"Server error: {result.StatusCode}"
            };
        }
        catch (Exception)
        {
            return new Response<int>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    public async Task<Response<int>> UpdatePassword(ResetPasswordModel request)
    {
        //  validate before hitting API
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return new Response<int>
            {
                Success = false,
                Message = "Password is required"
            };

        if (request.NewPassword != request.ConfirmPassword)
            return new Response<int>
            {
                Success = false,
                Message = "Passwords do not match"
            };

        try
        {
            var result = await _http.PostAsJsonAsync(
                "api/Auth/reset-password", request);

            var response = await result.Content
                .ReadFromJsonAsync<Response<int>>();

            return response ?? new Response<int>
            {
                Success = false,
                Message = result.IsSuccessStatusCode
                    ? "Unexpected response from server"
                    : $"Server error: {result.StatusCode}"
            };
        }
        catch (Exception)
        {
            return new Response<int>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }
}
