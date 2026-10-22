using MuuqWear.Application.Services.ContentService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.ContentItem;

namespace MuuqWear.Web.Services;

public sealed class FirestoreContentService : IContentService
{
    private readonly AdminFirestore _store;

    public FirestoreContentService(AdminFirestore store) => _store = store;

    public async Task<Response<List<ContentItemModel>>> GetAll(ContentCategory type) =>
        Response<List<ContentItemModel>>.SuccessResponse(await _store.ListContentAsync(type));

    public async Task<Response<ContentItemModel>> GetById(ContentCategory type, Guid id)
    {
        var item = (await _store.ListContentAsync(type)).FirstOrDefault(i => i.Id == id);
        return item is null
            ? Response<ContentItemModel>.Fail("Content not found")
            : Response<ContentItemModel>.SuccessResponse(item);
    }

    public Task<Response<ContentItemModel>> Create(ContentCategory type, CreateContentItemModel request) =>
        Upsert(type, new ContentItemModel
        {
            Title = request.Title,
            Content = request.Content,
            Status = request.Status ?? "draft",
            Category = request.Category,
            ImageUrl = request.ImageUrl,
            Author = request.Author,
            Excerpt = request.Excerpt
        });

    public async Task<Response<ContentItemModel>> Update(ContentCategory type, Guid id, UpdateContentItemModel request)
    {
        var existing = (await _store.ListContentAsync(type)).FirstOrDefault(i => i.Id == id)
            ?? new ContentItemModel { Id = id };
        existing.Title = request.Title;
        existing.Content = request.Content;
        existing.Status = request.Status ?? existing.Status;
        existing.ImageUrl = request.ImageUrl ?? existing.ImageUrl;
        existing.Author = request.Author ?? existing.Author;
        existing.Excerpt = request.Excerpt ?? existing.Excerpt;
        return await Upsert(type, existing);
    }

    public async Task<Response<bool>> Delete(ContentCategory type, Guid id) =>
        Response<bool>.SuccessResponse(await _store.DeleteContentAsync(id));

    public async Task<Response<ContentItemModel>> Publish(ContentCategory type, Guid id)
    {
        var item = (await _store.ListContentAsync(type)).FirstOrDefault(i => i.Id == id);
        if (item is null)
            return Response<ContentItemModel>.Fail("Content not found");
        item.Status = "published";
        item.PublishedAt = DateTime.UtcNow;
        return await Upsert(type, item);
    }

    public async Task<Response<ContentItemModel>> Unpublish(ContentCategory type, Guid id)
    {
        var item = (await _store.ListContentAsync(type)).FirstOrDefault(i => i.Id == id);
        if (item is null)
            return Response<ContentItemModel>.Fail("Content not found");
        item.Status = "draft";
        return await Upsert(type, item);
    }

    public Task<Response<string>> UploadImage(string fileName, byte[] bytes, string contentType) =>
        Task.FromResult(Response<string>.Fail("Upload images from the product editor for now."));

    public async Task<Response<List<ContentItemModel>>> GetPublishedDesignHistory() =>
        Response<List<ContentItemModel>>.SuccessResponse(
            (await _store.ListContentAsync(ContentCategory.DesignHistory))
            .Where(i => i.IsPublished)
            .ToList());

    private async Task<Response<ContentItemModel>> Upsert(ContentCategory type, ContentItemModel item) =>
        Response<ContentItemModel>.SuccessResponse(await _store.UpsertContentAsync(type, item));
}
