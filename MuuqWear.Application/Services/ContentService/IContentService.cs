using MuuqWear.Application.Shared;
using MuuqWear.Model.ContentItem;

namespace MuuqWear.Application.Services.ContentService;

public interface IContentService
{
    Task<Response<List<ContentItemModel>>> GetAll(ContentCategory type);
    Task<Response<ContentItemModel>> GetById(ContentCategory type, Guid id);
    Task<Response<ContentItemModel>> Create(ContentCategory type, CreateContentItemModel request);
    Task<Response<ContentItemModel>> Update(ContentCategory type, Guid id, UpdateContentItemModel request);
    Task<Response<bool>> Delete(ContentCategory type, Guid id);
    Task<Response<ContentItemModel>> Publish(ContentCategory type, Guid id);
    Task<Response<ContentItemModel>> Unpublish(ContentCategory type, Guid id);
    Task<Response<string>> UploadImage(string fileName, byte[] bytes, string contentType);
}
