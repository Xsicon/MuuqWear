using MuuqWear.Application.Shared;
using MuuqWear.Model.Muuqsimo;

namespace MuuqWear.Application.Services.MuuqsimoService;

public interface IMuuqsimoService
{
    Task<Response<MuuqsimoPageModel>> GetPageAsync(string? slug = null);
    Task<Response<List<MuuqsimoEventSummaryModel>>> GetPublishedEventsAsync();
}
