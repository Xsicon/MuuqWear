using MuuqWear.Application.Shared;
using MuuqWear.Model.AdminSettingsUser;

namespace MuuqWear.Application.Services.AdminUserService;
public interface IAdminSettingService
{
    Task<Response<List<AdminSettingsUserModel>>> GetAll();
    Task<Response<AdminSettingsUserModel>> Invite(InviteAdminSettingsUserModel request);
    Task<Response<AdminSettingsUserModel>> Update(Guid userId, UpdateAdminSettingsUserModel request);
    Task<Response<bool>> Deactivate(Guid userId);
    Task<Response<SupabaseHealthModel>> CheckSupabaseHealth();

}
