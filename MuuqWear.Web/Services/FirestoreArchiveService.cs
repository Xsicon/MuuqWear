using MuuqWear.Application.Services.ArchiveService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.ContentItem;

namespace MuuqWear.Web.Services;

public sealed class FirestoreArchiveService : IArchiveService
{
    private readonly AdminFirestore _store;

    public FirestoreArchiveService(AdminFirestore store) => _store = store;

    public async Task<Response<List<ContentItemModel>>> GetPublishedDesignHistoryAsync() =>
        Response<List<ContentItemModel>>.SuccessResponse(
            (await _store.ListContentAsync(ContentCategory.DesignHistory))
            .Where(i => i.IsPublished)
            .ToList());

    public async Task<Response<int>> RecordDesignHistoryViewAsync(Guid id)
    {
        var views = await _store.IncrementContentViewsAsync(id);
        return Response<int>.SuccessResponse(views);
    }
}
