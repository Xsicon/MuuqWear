using MuuqWear.Application.Shared;
using MuuqWear.Model.NotificationModel;

namespace MuuqWear.Application.Services.NotificationService;

public interface INotificationService
{
    Task<Response<List<NotificationModel>>> GetRecent();
    Task MarkAllRead();
}
