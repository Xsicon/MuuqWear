using MuuqWear.Application.Shared;
using MuuqWear.Model.ContentItem;
using MuuqWear.Model.Shared;

namespace MuuqWear.Application.Services.JournalService;

public interface IJournalService
{
    Task<Response<List<ContentItemModel>>> GetPublished();
    Task<Response<ContentItemModel>> GetById(Guid id);
    Task<Response<PaginatedResponse<ContentItemModel>>> GetPublished(
        int page = 1, int pageSize = 6, string? category = null);
}
