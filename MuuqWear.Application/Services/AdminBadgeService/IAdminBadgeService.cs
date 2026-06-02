using MuuqWear.Application.Shared;
using MuuqWear.Model.AdminBadgeCount;

namespace MuuqWear.Application.Services.AdminBadgeService;

public interface IAdminBadgeService
{
    Task<Response<AdminBadgeCountsModel>> GetCounts();
}
