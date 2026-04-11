using MuuqWear.Application.Shared;
using MuuqWear.Model.Authentication;

namespace MuuqWear.Application.Services.AuthService;
public interface IAuthService
{
    Task<Response<int>> Register(RegisterModel request);
    Task<Response<AuthResponseModel>> VerifyOTP(VerifyOTPModel request);
}
