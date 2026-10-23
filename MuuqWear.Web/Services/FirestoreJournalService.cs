using MuuqWear.Application.Services.JournalService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.ContentItem;
using MuuqWear.Model.Shared;

namespace MuuqWear.Web.Services;

public sealed class FirestoreJournalService : IJournalService
{
    private readonly AdminFirestore _store;

    public FirestoreJournalService(AdminFirestore store) => _store = store;

    public async Task<Response<List<ContentItemModel>>> GetPublished() =>
        Response<List<ContentItemModel>>.SuccessResponse(await Published());

    public async Task<Response<ContentItemModel>> GetById(Guid id)
    {
        var item = (await _store.ListContentAsync(ContentCategory.JournalArticles))
            .FirstOrDefault(i => i.Id == id);
        return item is null
            ? Response<ContentItemModel>.Fail("Article not found")
            : Response<ContentItemModel>.SuccessResponse(item);
    }

    public async Task<Response<PaginatedResponse<ContentItemModel>>> GetPublished(
        int page = 1, int pageSize = 6, string? category = null)
    {
        var all = await Published();
        if (!string.IsNullOrWhiteSpace(category))
            all = all.Where(i => string.Equals(i.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
        return Response<PaginatedResponse<ContentItemModel>>.SuccessResponse(
            Page(all, page, pageSize));
    }

    private async Task<List<ContentItemModel>> Published() =>
        (await _store.ListContentAsync(ContentCategory.JournalArticles))
        .Where(i => i.IsPublished)
        .ToList();

    private static PaginatedResponse<ContentItemModel> Page(List<ContentItemModel> all, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 6;
        var total = all.Count;
        var pages = (int)Math.Ceiling(total / (double)pageSize);
        return new PaginatedResponse<ContentItemModel>
        {
            Data = all.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = pages
        };
    }
}
