using MuuqWear.Application.Shared;
using MuuqWear.Model.Muuqsimo;

namespace MuuqWear.Application.Services.MuuqsimoService;

public class MuuqsimoService : IMuuqsimoService
{
    private readonly HttpClient _http;

    public MuuqsimoService(HttpClient http)
    {
        _http = http;
    }

    public async Task<Response<MuuqsimoPageModel>> GetPageAsync(string? slug = null)
    {
        try
        {
            var url = string.IsNullOrWhiteSpace(slug)
                ? "api/Muuqsimo"
                : $"api/Muuqsimo?slug={Uri.EscapeDataString(slug)}";

            var result = await _http.GetAsync(url);
            return await HttpResponseReader.ReadAsync<MuuqsimoPageModel>(result);
        }
        catch (Exception ex)
        {
            return Response<MuuqsimoPageModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    public async Task<Response<List<MuuqsimoEventSummaryModel>>> GetPublishedEventsAsync()
    {
        try
        {
            var result = await _http.GetAsync("api/Muuqsimo/events");
            return await HttpResponseReader.ReadAsync<List<MuuqsimoEventSummaryModel>>(result);
        }
        catch (Exception ex)
        {
            return Response<List<MuuqsimoEventSummaryModel>>.Fail(HttpResponseReader.FromException(ex));
        }
    }
}
