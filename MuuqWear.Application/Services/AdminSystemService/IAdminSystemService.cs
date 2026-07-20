using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.AdminSystem;

namespace MuuqWear.Application.Services.AdminSystemService;

public interface IAdminSystemService
{
    Task<Response<SystemHealthOverviewModel>> GetOverviewAsync();
    Task<Response<List<IntegrationStatusModel>>> GetIntegrationsAsync();
    Task<Response<IntegrationStatusModel>> TestIntegrationAsync(string name);
    Task<Response<IntegrationStatusModel>> ReconnectIntegrationAsync(string name);
    Task<Response<SyncJobResultModel>> RunSyncAsync(string jobKey);
    Task<Response<SyncJobResultModel>> GetSyncJobAsync(Guid id);
    Task<Response<SystemLogsPageModel>> GetLogsAsync(
        int days,
        string level,
        string? search,
        int page,
        int pageSize);
    Task<Response<List<BackgroundJobModel>>> GetJobsAsync();
    Task<Response<BackgroundJobModel>> GetJobAsync(Guid id);
}
