using MuuqWear.Application.Services.AdminSystemService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.AdminSystem;

namespace MuuqWear.Web.Services;

public sealed class FirestoreAdminSystemService : IAdminSystemService
{
    private readonly AdminFirestore _store;
    private readonly IConfiguration _config;

    public FirestoreAdminSystemService(AdminFirestore store, IConfiguration config)
    {
        _store = store;
        _config = config;
    }

    public async Task<Response<SystemHealthOverviewModel>> GetOverviewAsync()
    {
        var firestoreOk = await _store.PingAsync();
        var stripeOk = HasConfig("Stripe") || HasConfig("STRIPE");
        return Response<SystemHealthOverviewModel>.SuccessResponse(new SystemHealthOverviewModel
        {
            DatabaseIsHealthy = firestoreOk,
            DatabaseStatus = firestoreOk ? "Firestore connected" : "Firestore unreachable",
            StripeIsHealthy = stripeOk,
            StripeStatus = stripeOk ? "Processing payments" : "Stripe keys not loaded in this instance",
            SupabaseIsHealthy = firestoreOk,
            SupabaseStatus = firestoreOk ? "Firebase Auth + Firestore" : "Firebase unreachable",
            LastBackupDisplay = "Managed by Google Cloud",
            ActiveUsersCount = await _store.CountCustomersAsync(),
            CheckedAt = DateTime.UtcNow
        });
    }

    public async Task<Response<List<IntegrationStatusModel>>> GetIntegrationsAsync()
    {
        var overview = await GetOverviewAsync();
        var firestoreOk = overview.Data?.DatabaseIsHealthy == true;
        var stripeOk = overview.Data?.StripeIsHealthy == true;
        var postmarkOk = HasConfig("Postmark") || !string.IsNullOrWhiteSpace(_config["FunctionsBaseUrl"]);
        return Response<List<IntegrationStatusModel>>.SuccessResponse(
        [
            new() { Name = "Stripe", IsHealthy = stripeOk, StatusDetail = overview.Data?.StripeStatus ?? "", SupportsLiveHealthCheck = true, LastCheckedAt = DateTime.UtcNow },
            new() { Name = "Firebase", IsHealthy = firestoreOk, StatusDetail = firestoreOk ? "Auth, Firestore, and Hosting" : "Unreachable", SupportsLiveHealthCheck = true, LastCheckedAt = DateTime.UtcNow },
            new() { Name = "Postmark", IsHealthy = postmarkOk, StatusDetail = postmarkOk ? "Transactional email" : "Not configured", SupportsLiveHealthCheck = false, LastCheckedAt = DateTime.UtcNow }
        ]);
    }

    public async Task<Response<IntegrationStatusModel>> TestIntegrationAsync(string name)
    {
        var all = await GetIntegrationsAsync();
        var match = all.Data?.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return match is null
            ? Response<IntegrationStatusModel>.Fail("Unknown integration")
            : Response<IntegrationStatusModel>.SuccessResponse(match);
    }

    public Task<Response<IntegrationStatusModel>> ReconnectIntegrationAsync(string name) =>
        TestIntegrationAsync(name);

    public Task<Response<SyncJobResultModel>> RunSyncAsync(string jobKey) =>
        Task.FromResult(Response<SyncJobResultModel>.SuccessResponse(new SyncJobResultModel
        {
            Id = Guid.NewGuid(),
            JobKey = jobKey,
            Status = "completed",
            Message = "Catalog already lives in Firestore.",
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            RecordsAffected = 0
        }));

    public Task<Response<SyncJobResultModel>> GetSyncJobAsync(Guid id) =>
        Task.FromResult(Response<SyncJobResultModel>.SuccessResponse(new SyncJobResultModel
        {
            Id = id,
            Status = "completed",
            Message = "No queued sync jobs.",
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        }));

    public Task<Response<SystemLogsPageModel>> GetLogsAsync(
        int days, string level, string? search, int page, int pageSize) =>
        Task.FromResult(Response<SystemLogsPageModel>.SuccessResponse(new SystemLogsPageModel
        {
            Items = [],
            TotalCount = 0,
            Page = page,
            PageSize = pageSize
        }));

    public Task<Response<List<BackgroundJobModel>>> GetJobsAsync() =>
        Task.FromResult(Response<List<BackgroundJobModel>>.SuccessResponse(
        [
            new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Affiliate commission recalculation", Schedule = "Hourly", Status = "idle", LastRunMessage = "No pending affiliate commissions" },
            new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Daily database backup", Schedule = "Daily", Status = "idle", LastRunMessage = "Handled by Google Cloud Firestore" },
            new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Inventory ERP sync", Schedule = "Manual", Status = "idle", LastRunMessage = "Not connected" },
            new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Stripe order sync", Schedule = "On checkout", Status = "idle", LastRunMessage = "Orders written by Firebase Functions" }
        ]));

    public async Task<Response<BackgroundJobModel>> GetJobAsync(Guid id)
    {
        var jobs = await GetJobsAsync();
        var match = jobs.Data?.FirstOrDefault(j => j.Id == id);
        return match is null
            ? Response<BackgroundJobModel>.Fail("Job not found")
            : Response<BackgroundJobModel>.SuccessResponse(match);
    }

    private bool HasConfig(string prefix) =>
        _config.AsEnumerable().Any(pair =>
            pair.Key.Contains(prefix, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(pair.Value));
}
