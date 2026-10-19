using System.Net.Http.Json;
using MuuqWear.Application.Services.AuthService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Authentication;

namespace MuuqWear.Client.Auth;

public sealed class BrowserAuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly BrowserTokenStore _tokens;

    public BrowserAuthService(HttpClient http, BrowserTokenStore tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    public Task<bool> IsUserLoggedIn() =>
        Task.FromResult(!string.IsNullOrWhiteSpace(_tokens.AccessToken));

    public Task<Response<int>> Register(RegisterModel request) =>
        Post<int>("api/Auth/register", request);

    public async Task<Response<AuthResponseModel>> VerifyOTP(VerifyOTPModel request)
    {
        var response = await Post<AuthResponseModel>("api/Auth/verifyotp", request);
        if (response.Success && response.Data is not null)
            await _tokens.SaveAsync(response.Data);
        return response;
    }

    public async Task<Response<AuthResponseModel>> Login(LoginModel request)
    {
        var response = await Post<AuthResponseModel>("api/Auth/login", request);
        if (response.Success && response.Data is not null)
            await _tokens.SaveAsync(response.Data);
        return response;
    }

    public Task<Response<int>> Logout() =>
        Post<int>("api/Auth/logout", new { });

    public Task<Response<int>> SendMagicLink(MagicLinkModel request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Task.FromResult(Response<int>.Fail("Please enter your email"));
        return Post<int>("api/Auth/magic-link", request);
    }

    public async Task<Response<AuthResponseModel>> VerifyMagicLink(MagicLinkVerifyModel request)
    {
        var response = await Post<AuthResponseModel>("api/Auth/verify-magic-link", request);
        if (response.Success && response.Data is not null)
            await _tokens.SaveAsync(response.Data);
        return response;
    }

    public async Task<Response<string>> GetGoogleSignInUrl(string? redirectTo = null)
    {
        var url = "api/Auth/google-signin-url";
        if (!string.IsNullOrWhiteSpace(redirectTo))
            url += "?redirectTo=" + Uri.EscapeDataString(redirectTo);

        try
        {
            return await _http.GetFromJsonAsync<Response<string>>(url)
                ?? Response<string>.Fail("Unexpected response from server");
        }
        catch
        {
            return Response<string>.Fail("Unable to connect to server. Please try again.");
        }
    }

    public Task<Response<int>> SendPasswordReset(ForgotPasswordModel request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Task.FromResult(Response<int>.Fail("Email is required"));
        return Post<int>("api/Auth/forgot-password", request);
    }

    public Task<Response<int>> UpdatePassword(ResetPasswordModel request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return Task.FromResult(Response<int>.Fail("Password is required"));
        if (request.NewPassword != request.ConfirmPassword)
            return Task.FromResult(Response<int>.Fail("Passwords do not match"));
        return Post<int>("api/Auth/reset-password", request);
    }

    private async Task<Response<T>> Post<T>(string url, object body)
    {
        try
        {
            var result = await _http.PostAsJsonAsync(url, body);
            return await result.Content.ReadFromJsonAsync<Response<T>>()
                ?? Response<T>.Fail(result.IsSuccessStatusCode
                    ? "Unexpected response from server"
                    : $"Server error: {result.StatusCode}");
        }
        catch
        {
            return Response<T>.Fail("Unable to connect to server. Please try again.");
        }
    }
}
