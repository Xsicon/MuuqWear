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
    public Task<Response<AuthResponseModel>> VerifyOTP(VerifyOTPModel request) =>
        Task.FromResult(Response<AuthResponseModel>.Fail("Enter the authenticator code on the sign-in screen."));

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
    public Task<Response<int>> Logout() =>
        Task.FromResult(Response<int>.SuccessResponse(1));

    public Task<Response<int>> SendMagicLink(MagicLinkModel request) =>
        SendPasswordReset(new ForgotPasswordModel { Email = request.Email });

    public Task<Response<AuthResponseModel>> VerifyMagicLink(MagicLinkVerifyModel request) =>
        Task.FromResult(Response<AuthResponseModel>.Fail("Open the reset email, then sign in with your new password."));

    public Task<Response<string>> GetGoogleSignInUrl(string? redirectTo = null) =>
        Task.FromResult(Response<string>.Fail("Sign in with email and password."));

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
            var apiKey = _config["Firebase:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return Response<int>.Fail("Unable to send the reset email.");
            using var result = await _http.PostAsJsonAsync(
                IdentityUrl("accounts:sendOobCode", apiKey),
                new { requestType = "PASSWORD_RESET", email = request.Email });
            if (!result.IsSuccessStatusCode)
                return Response<int>.Fail("Unable to send the reset email.");
            return Response<int>.SuccessResponse(1, "Check your email");
        }
        catch (Exception)
        {
            return Response<int>.Fail("Unable to send the reset email.");
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

        return Response<int>.Fail("Use the reset link from your email.");
    }
}
