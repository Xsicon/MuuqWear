using MuuqWear.Application.Shared;
using MuuqWear.Model.ContentItem;

namespace MuuqWear.Application.Services.ArchiveService;

public interface IArchiveService
{
    Task<Response<List<ContentItemModel>>> GetPublishedDesignHistoryAsync();
    Task<Response<int>> RecordDesignHistoryViewAsync(Guid id);
}
