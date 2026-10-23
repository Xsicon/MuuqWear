using MuuqWear.Application.Services.NotificationService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.NotificationModel;

namespace MuuqWear.Web.Services;

public sealed class LocalNotificationService : INotificationService
{
    public Task<Response<List<NotificationModel>>> GetRecent() =>
        Task.FromResult(Response<List<NotificationModel>>.SuccessResponse([]));

    public Task MarkAllRead() => Task.CompletedTask;
}
