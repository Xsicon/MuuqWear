using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.AdminSystem;

namespace MuuqWear.Application.Services.AdminSystemService;

public class AdminSystemService : IAdminSystemService
{
    private readonly HttpClient _http;

    public AdminSystemService(HttpClient http)
    {
        _http = http;
    }

    public async Task<Response<SystemHealthOverviewModel>> GetOverviewAsync() =>
        await GetAsync<SystemHealthOverviewModel>("api/AdminSystem/overview");

    public async Task<Response<List<IntegrationStatusModel>>> GetIntegrationsAsync() =>
        await GetAsync<List<IntegrationStatusModel>>("api/AdminSystem/integrations");

    public async Task<Response<IntegrationStatusModel>> TestIntegrationAsync(string name)
    {
        var encoded = Uri.EscapeDataString(name);
        return await PostAsync<IntegrationStatusModel>($"api/AdminSystem/integrations/{encoded}/test");
    }

    public async Task<Response<IntegrationStatusModel>> ReconnectIntegrationAsync(string name)
    {
        var encoded = Uri.EscapeDataString(name);
        return await PostAsync<IntegrationStatusModel>($"api/AdminSystem/integrations/{encoded}/reconnect");
    }

    public async Task<Response<SyncJobResultModel>> RunSyncAsync(string jobKey)
    {
        var encoded = Uri.EscapeDataString(jobKey);
        return await PostAsync<SyncJobResultModel>($"api/AdminSystem/sync/{encoded}");
    }

    public async Task<Response<SyncJobResultModel>> GetSyncJobAsync(Guid id) =>
        await GetAsync<SyncJobResultModel>($"api/AdminSystem/sync/{id}");

    public async Task<Response<SystemLogsPageModel>> GetLogsAsync(
        int days,
        string level,
        string? search,
        int page,
        int pageSize)
    {
        var query = new List<string>
        {
            $"days={days}",
            $"level={Uri.EscapeDataString(level)}",
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(search))
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");

        return await GetAsync<SystemLogsPageModel>($"api/AdminSystem/logs?{string.Join('&', query)}");
    }

    public async Task<Response<List<BackgroundJobModel>>> GetJobsAsync() =>
        await GetAsync<List<BackgroundJobModel>>("api/AdminSystem/jobs");

    public async Task<Response<BackgroundJobModel>> GetJobAsync(Guid id) =>
        await GetAsync<BackgroundJobModel>($"api/AdminSystem/jobs/{id}");

    private async Task<Response<T>> GetAsync<T>(string url)
    {
        try
        {
            var response = await _http.GetAsync(url);
            return await HttpResponseReader.ReadAsync<T>(response);
        }
        catch (Exception ex)
        {
            return Response<T>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    private async Task<Response<T>> PostAsync<T>(string url)
    {
        try
        {
            var response = await _http.PostAsync(url, null);
            return await HttpResponseReader.ReadAsync<T>(response);
        }
        catch (Exception ex)
        {
            return Response<T>.Fail(HttpResponseReader.FromException(ex));
        }
    }
}
