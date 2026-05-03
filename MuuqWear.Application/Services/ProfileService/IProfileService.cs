using MuuqWear.Application.Shared;
using MuuqWear.Model.Profile;

namespace MuuqWear.Application.Interfaces;

public interface IProfileService
{
    Task<Response<ProfileModel>> GetProfile();
    Task<Response<ProfileModel>> UpdateProfile(UpdateProfileModel request);
    Task<Response<bool>> DeleteAccount();
}