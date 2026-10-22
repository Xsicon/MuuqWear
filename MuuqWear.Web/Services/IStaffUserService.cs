using MuuqWear.Application.Shared;
using MuuqWear.Model.Staff;

namespace MuuqWear.Web.Services;

public interface IStaffUserService
{
    Task<Response<List<StaffUserModel>>> ListAsync(CancellationToken cancellationToken = default);
    Task<Response<StaffUserModel>> GetAsync(string uid, CancellationToken cancellationToken = default);
    Task<Response<StaffUserModel>> CreateAsync(CreateStaffUserModel request, CancellationToken cancellationToken = default);
    Task<Response<StaffUserModel>> UpdateAsync(string uid, UpdateStaffUserModel request, CancellationToken cancellationToken = default);
    Task<Response<bool>> SendPasswordResetAsync(string uid, CancellationToken cancellationToken = default);
}
