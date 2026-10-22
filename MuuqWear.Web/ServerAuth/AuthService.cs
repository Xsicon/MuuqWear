using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.Configuration;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Authentication;
using MuuqWear.Model.OrderReturn;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MuuqWear.Application.Services.AuthService;
public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly ProtectedSessionStorage _sessionStorage;
    private readonly IConfiguration _config;
    private readonly StaffSessionJwt _staffJwt;
    public bool IsNewSignup { get; private set; } = false;


    public AuthService(HttpClient http, ProtectedSessionStorage sessionStorage, IConfiguration config, StaffSessionJwt staffJwt)
    {
        this._http = http;
        this._sessionStorage = sessionStorage;
        _config = config;
        _staffJwt = staffJwt;
    }
    public async Task<Response<int>> Register(RegisterModel request)
    {
        var apiKey = _config["Firebase:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Response<int>.Fail("Unable to create the account. Please try again.");
        }

        using var signup = await _http.PostAsJsonAsync(
            IdentityUrl("accounts:signUp", apiKey),
            new
            {
                email = request.Email,
                password = request.Password,
                displayName = request.FullName,
                returnSecureToken = true
            });
        var signupBody = await signup.Content.ReadFromJsonAsync<IdentityResponse>();
        if (!signup.IsSuccessStatusCode)
        {
            var code = signupBody?.Error?.Message ?? "";
            if (code.StartsWith("EMAIL_EXISTS", StringComparison.OrdinalIgnoreCase))
                return Response<int>.Fail("An account with that email already exists. Please sign in.");
            if (code.StartsWith("WEAK_PASSWORD", StringComparison.OrdinalIgnoreCase))
                return Response<int>.Fail("Choose a stronger password.");
            return Response<int>.Fail("Unable to create the account. Please try again.");
        }

        if (!string.IsNullOrWhiteSpace(signupBody?.IdToken))
        {
            await _http.PostAsJsonAsync(
                IdentityUrl("accounts:sendOobCode", apiKey),
                new { requestType = "VERIFY_EMAIL", idToken = signupBody.IdToken });
        }

        return Response<int>.SuccessResponse(1, "Check your email to confirm your account.");
    }

    private static Uri IdentityUrl(string method, string apiKey) =>
        new($"https://identitytoolkit.googleapis.com/v1/{method}?key={Uri.EscapeDataString(apiKey)}");

    private sealed class IdentityResponse
    {
        [JsonPropertyName("idToken")]
        public string? IdToken { get; set; }
        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }
        [JsonPropertyName("localId")]
        public string? LocalId { get; set; }
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }
        [JsonPropertyName("error")]
        public IdentityErrorBody? Error { get; set; }
    }

    private sealed class IdentityErrorBody
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }
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
            var apiKey = _config["Firebase:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return new Response<AuthResponseModel>
                {
                    Success = false,
                    Message = "Invalid email or password."
                };
            }

            using var signin = await _http.PostAsJsonAsync(
                IdentityUrl("accounts:signInWithPassword", apiKey),
                new
                {
                    email = request.Email,
                    password = request.Password,
                    returnSecureToken = true
                });
            var body = await signin.Content.ReadFromJsonAsync<IdentityResponse>();
            if (!signin.IsSuccessStatusCode || string.IsNullOrWhiteSpace(body?.IdToken))
            {
                return new Response<AuthResponseModel>
                {
                    Success = false,
                    Message = "Invalid email or password."
                };
            }

            var email = body.Email ?? request.Email;
            var uid = body.LocalId ?? "";
            var role = FirebaseIdTokenRole.FromAccessToken(body.IdToken);
            var name = body.DisplayName ?? "";
            return new Response<AuthResponseModel>
            {
                Success = true,
                Message = "Login Successful",
                Data = new AuthResponseModel
                {
                    AccessToken = _staffJwt.Create(uid, email, role, name),
                    RefreshToken = null,
                    Email = email,
                    UserId = uid,
                    UserName = name,
                    Role = role
                }
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

    public async Task<Response<string>> GetGoogleSignInUrl(string? redirectTo = null)
    {
        try
        {
            var url = "api/Auth/google-signin-url";
            if (!string.IsNullOrWhiteSpace(redirectTo))
                url += $"?redirectTo={Uri.EscapeDataString(redirectTo)}";

            var response = await _http
                .GetFromJsonAsync<Response<string>>(url);

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
