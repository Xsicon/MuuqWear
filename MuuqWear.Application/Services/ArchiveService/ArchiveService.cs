using MuuqWear.Application.Shared;
using MuuqWear.Model.ContentItem;

namespace MuuqWear.Application.Services.ArchiveService;

public class ArchiveService : IArchiveService
{
    private readonly HttpClient _http;

    public ArchiveService(HttpClient http)
    {
        _http = http;
    }

    public async Task<Response<List<ContentItemModel>>> GetPublishedDesignHistoryAsync()
    {
        try
        {
            var result = await _http.GetAsync("api/Content/design-history/published");
            return await HttpResponseReader.ReadAsync<List<ContentItemModel>>(result);
        }
        catch (Exception ex)
        {
            return Response<List<ContentItemModel>>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    public async Task<Response<int>> RecordDesignHistoryViewAsync(Guid id)
    {
        try
        {
            var result = await _http.PostAsync($"api/Content/design-history/{id}/view", null);
            return await HttpResponseReader.ReadAsync<int>(result);
        }
        catch (Exception ex)
        {
            return Response<int>.Fail(HttpResponseReader.FromException(ex));
        }
    }
}
