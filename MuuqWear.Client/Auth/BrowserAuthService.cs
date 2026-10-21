using MuuqWear.Application.Services.AuthService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Authentication;

namespace MuuqWear.Client.Auth;

public sealed class BrowserAuthService : IAuthService
{
    private readonly FirebaseSession _firebase;
    private readonly BrowserTokenStore _tokens;

    public BrowserAuthService(FirebaseSession firebase, BrowserTokenStore tokens)
    {
        _firebase = firebase;
        _tokens = tokens;
    }

    public Task<bool> IsUserLoggedIn() =>
        Task.FromResult(!string.IsNullOrWhiteSpace(_tokens.AccessToken));

    public async Task<Response<int>> Register(RegisterModel request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Response<int>.Fail("Email and password are required");
        if (request.Password != request.ConfirmPassword)
            return Response<int>.Fail("Passwords do not match");

        var result = await _firebase.SignUpAsync(request.Email, request.Password, request.FullName);
        if (result.Ok)
            return Response<int>.SuccessResponse(1, "Check your email to confirm your account.");

        if (!IsEmailAlreadyUsed(result.Message))
            return Response<int>.Fail(result.Message ?? "Unable to create the account.");

        var existing = await _firebase.SignInAsync(request.Email, request.Password);
        if (!existing.Ok)
            return Response<int>.Fail("An account with that email already exists. Please sign in.");
        if (existing.EmailVerified)
            return Response<int>.Fail("An account with that email already exists. Please sign in.");

        var resent = await _firebase.SendEmailVerificationAsync();
        if (!resent.Ok)
            return Response<int>.Fail(resent.Message ?? "Unable to send the confirmation email.");
        return Response<int>.SuccessResponse(1, "Check your email to confirm your account.");
    }

    private static bool IsEmailAlreadyUsed(string? message) =>
        !string.IsNullOrWhiteSpace(message) &&
        message.Contains("already exists", StringComparison.OrdinalIgnoreCase);

    public async Task<Response<AuthResponseModel>> Login(LoginModel request)
    {
        var result = await _firebase.SignInAsync(request.Email ?? "", request.Password ?? "");
        if (result.MfaRequired)
            return Response<AuthResponseModel>.Fail("Enter your authenticator code");
        if (!result.Ok)
            return Response<AuthResponseModel>.Fail(result.Message ?? "Email or password is incorrect.");
        var session = await StoreSessionAsync(result);
        return Response<AuthResponseModel>.SuccessResponse(session);
    }

    public Task<Response<AuthResponseModel>> VerifyOTP(VerifyOTPModel request)
    {
        return CompleteMfaAsync(request.Otp);
    }

    public async Task<Response<int>> Logout()
    {
        await _firebase.SignOutAsync();
        await _tokens.SaveAsync(new AuthResponseModel());
        return Response<int>.SuccessResponse(1);
    }

    public Task<Response<int>> SendMagicLink(MagicLinkModel request) =>
        SendPasswordReset(new ForgotPasswordModel { Email = request.Email });

    public Task<Response<AuthResponseModel>> VerifyMagicLink(MagicLinkVerifyModel request) =>
        Task.FromResult(Response<AuthResponseModel>.Fail("Open the reset email, then sign in with your new password."));

    public Task<Response<string>> GetGoogleSignInUrl(string? redirectTo = null) =>
        Task.FromResult(Response<string>.Fail("Sign in with email and password."));

    public async Task<Response<int>> SendPasswordReset(ForgotPasswordModel request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Response<int>.Fail("Email is required");
        var result = await _firebase.SendPasswordResetAsync(request.Email);
        if (!result.Ok)
            return Response<int>.Fail(result.Message ?? "Unable to send the reset email.");
        return Response<int>.SuccessResponse(1, "Check your email");
    }

    public Task<Response<int>> UpdatePassword(ResetPasswordModel request) =>
        Task.FromResult(Response<int>.Fail("Use the reset email from Firebase Auth."));

    private async Task<Response<AuthResponseModel>> CompleteMfaAsync(string? code)
    {
        var result = await _firebase.CompleteTotpSignInAsync(code ?? "");
        if (!result.Ok)
            return Response<AuthResponseModel>.Fail(result.Message ?? "That authenticator code is not valid.");
        var session = await StoreSessionAsync(result);
        return Response<AuthResponseModel>.SuccessResponse(session);
    }

    private async Task<AuthResponseModel> StoreSessionAsync(FirebaseAuthResult result)
    {
        var token = await _firebase.GetIdTokenAsync();
        var session = new AuthResponseModel
        {
            AccessToken = token,
            RefreshToken = "firebase",
            Email = result.Email,
            UserId = result.Uid,
            UserName = result.Email,
            Role = "Customer"
        };
        await _tokens.SaveAsync(session);
        return session;
    }
}
