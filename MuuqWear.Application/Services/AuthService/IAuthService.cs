using MuuqWear.Application.Shared;
using MuuqWear.Model.Authentication;

namespace MuuqWear.Application.Services.AuthService;
public interface IAuthService
{
    Task<Response<int>> Register(RegisterModel request);
    Task<Response<AuthResponseModel>> VerifyOTP(VerifyOTPModel request);
    Task<bool> IsUserLoggedIn();
    Task<Response<AuthResponseModel>> Login(LoginModel request);
    Task<Response<int>> Logout();
    Task<Response<int>> SendMagicLink(MagicLinkModel request);
    Task<Response<AuthResponseModel>> VerifyMagicLink(MagicLinkVerifyModel request);
    Task<Response<string>> GetGoogleSignInUrl();
    Task<Response<int>> SendPasswordReset(ForgotPasswordModel request);
    Task<Response<int>> UpdatePassword(ResetPasswordModel request);
}
