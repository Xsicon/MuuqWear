using MuuqWear.Application.Services.JournalService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.ContentItem;
using MuuqWear.Model.Shared;

namespace MuuqWear.Client.Catalog;

public sealed class BrowserJournalService : IJournalService
{
    private readonly BrowserCatalog _catalog;

    public BrowserJournalService(BrowserCatalog catalog) => _catalog = catalog;

    public async Task<Response<List<ContentItemModel>>> GetPublished() =>
        Response<List<ContentItemModel>>.SuccessResponse(await _catalog.JournalAsync());

    public async Task<Response<ContentItemModel>> GetById(Guid id)
    {
        var item = (await _catalog.JournalAsync()).FirstOrDefault(i => i.Id == id);
        return item is null
            ? Response<ContentItemModel>.Fail("Article not found")
            : Response<ContentItemModel>.SuccessResponse(item);
    }

    public async Task<Response<PaginatedResponse<ContentItemModel>>> GetPublished(
        int page = 1, int pageSize = 6, string? category = null)
    {
        var articles = await _catalog.JournalAsync();
        if (!string.IsNullOrWhiteSpace(category))
            articles = articles.Where(i => string.Equals(i.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 6;
        var total = articles.Count;
        var pages = (int)Math.Ceiling(total / (double)pageSize);
        return Response<PaginatedResponse<ContentItemModel>>.SuccessResponse(new PaginatedResponse<ContentItemModel>
        {
            Data = articles.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = pages
        });
    }
}
