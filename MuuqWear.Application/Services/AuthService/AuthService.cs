using MuuqWear.Application.Shared;
using MuuqWear.Model.Authentication;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace MuuqWear.Application.Services.AuthService;
public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly ProtectedSessionStorage _sessionStorage;

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
        var result = await _http.PostAsJsonAsync("api/Auth/login", request);
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

    public async Task<Response<int>> Logout()
    {
        try
        {
            var result = await _http.PostAsync("api/Auth/logout", null);
            if (!result.IsSuccessStatusCode)
            {
                return new Response<int>
                {
                    Success = false,
                    Message = $"Server error: {result.StatusCode}"
                };
            }
            var response = await result.Content.ReadFromJsonAsync<Response<int>>();
            return response ?? new Response<int>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<int>
            {
                Success = false,
                Message = ex.Message
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

            if (!result.IsSuccessStatusCode)
            {
                // try read error message from response body
                var errorResponse = await result.Content
                    .ReadFromJsonAsync<Response<int>>();

                return errorResponse ?? new Response<int>
                {
                    Success = false,
                    Message = $"Server error: {result.StatusCode}"
                };
            }

            var response = await result.Content
                .ReadFromJsonAsync<Response<int>>();

            return response ?? new Response<int>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<int>
            {
                Success = false,
                Message = ex.Message
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

            if (!result.IsSuccessStatusCode)
            {
                var errorResponse = await result.Content
                    .ReadFromJsonAsync<Response<AuthResponseModel>>();
                return errorResponse ?? new Response<AuthResponseModel>
                {
                    Success = false,
                    Message = $"Server error: {result.StatusCode}"
                };
            }

            var response = await result.Content
                .ReadFromJsonAsync<Response<AuthResponseModel>>();

            return response ?? new Response<AuthResponseModel>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<AuthResponseModel>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<Response<string>> GetGoogleSignInUrl()
    {
        try
        {
            // GET api/Auth/google-signin-url
            // no body needed — just GET request 
            var result = await _http
                .GetFromJsonAsync<Response<string>>("api/Auth/google-signin-url");

            return result ?? new Response<string>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<string>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<Response<int>> SendPasswordReset(ForgotPasswordModel request)
    {
        try
        {
            // validate before API call 
            if (string.IsNullOrWhiteSpace(request.Email))
                return new Response<int>
                {
                    Success = false,
                    Message = "Email is required"
                };

            var result = await _http.PostAsJsonAsync(
                "api/Auth/forgot-password", request);

            if (!result.IsSuccessStatusCode)
            {
                var errorResponse = await result.Content
                    .ReadFromJsonAsync<Response<int>>();
                return errorResponse ?? new Response<int>
                {
                    Success = false,
                    Message = $"Server error: {result.StatusCode}"
                };
            }

            var response = await result.Content
                .ReadFromJsonAsync<Response<int>>();

            return response ?? new Response<int>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<int>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<Response<int>> UpdatePassword(ResetPasswordModel request)
    {
        try
        {
            // validate before API call 
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

            var result = await _http.PostAsJsonAsync(
                "api/Auth/reset-password", request);

            if (!result.IsSuccessStatusCode)
            {
                var errorResponse = await result.Content
                    .ReadFromJsonAsync<Response<int>>();
                return errorResponse ?? new Response<int>
                {
                    Success = false,
                    Message = $"Server error: {result.StatusCode}"
                };
            }

            var response = await result.Content
                .ReadFromJsonAsync<Response<int>>();

            return response ?? new Response<int>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<int>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
}
