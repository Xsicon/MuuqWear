using Microsoft.AspNetCore.Components;
using MuuqWear.Application.Services.JobPostingService;
using MuuqWear.Model.JobApplication;
using MuuqWear.Model.JobPosting;
using MuuqWear.Web.Services;

namespace MuuqWear.Web.Components.Pages.AdminComponent.Careers;

public partial class AdminCareerApplicationsTab
{
    [Parameter] public Guid? JobFilterId { get; set; }
    [Parameter] public EventCallback<int> OnCountsChanged { get; set; }

    [Inject] private IJobPostingService JobPostingService { get; set; } = default!;

    private sealed record InboxItem(JobApplicationModel Application, string JobTitle);

    private List<JobPostingModel> jobs = [];
    private List<InboxItem> allItems = [];
    private List<InboxItem> filteredItems = [];
    private bool isLoading = true;
    private string? loadError;
    private string jobFilter = string.Empty;
    private string statusFilter = "all";
    private string search = string.Empty;
    private string? actionError;

    private JobApplicationModel? detailApp;
    private string detailStatus = "new";
    private string detailNotes = string.Empty;
    private bool isSavingDetail;
    private string detailError = string.Empty;

    protected override async Task OnInitializedAsync() => await LoadInbox();

    protected override void OnParametersSet()
    {
        SyncJobFilterFromParameter();
        ApplyFilters();
    }

    private void SyncJobFilterFromParameter() =>
        jobFilter = JobFilterId.HasValue ? JobFilterId.Value.ToString() : string.Empty;

    private async Task LoadInbox()
    {
        isLoading = true;
        actionError = null;
        loadError = null;
        StateHasChanged();

        try
        {
            var jobsResult = await JobPostingService.GetAll();
            if (!jobsResult.Success || jobsResult.Data == null)
            {
                jobs = [];
                allItems = [];
                loadError = AdminUiErrorHelper.FromApi(jobsResult.Message, "Failed to load job postings.");
                ApplyFilters();
                await NotifyCounts();
                return;
            }

            jobs = jobsResult.Data;

            var tasks = jobs.Select(async job =>
            {
                var appsResult = await JobPostingService.GetApplicationsByJob(job.Id);
                var apps = appsResult.Success && appsResult.Data != null ? appsResult.Data : [];
                return apps.Select(app => new InboxItem(app, job.Title));
            });

            var results = await Task.WhenAll(tasks);
            allItems = results.SelectMany(x => x).OrderByDescending(x => x.Application.CreatedAt).ToList();
        }
        catch (Exception ex)
        {
            jobs = [];
            allItems = [];
            loadError = AdminUiErrorHelper.FromException(ex);
        }
        finally
        {
            SyncJobFilterFromParameter();
            ApplyFilters();
            await NotifyCounts();
            isLoading = false;
            StateHasChanged();
        }
    }

    private void OnSearchInput(ChangeEventArgs e)
    {
        search = e.Value?.ToString() ?? string.Empty;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        IEnumerable<InboxItem> query = allItems;

        if (!string.IsNullOrEmpty(jobFilter) && Guid.TryParse(jobFilter, out var jobId))
            query = query.Where(x => x.Application.JobId == jobId);

        if (statusFilter != "all")
            query = query.Where(x => x.Application.Status == statusFilter);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            query = query.Where(x => x.Application.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        filteredItems = query.ToList();
    }

    private async Task NotifyCounts()
    {
        var newCount = allItems.Count(x => x.Application.Status == "new");
        if (OnCountsChanged.HasDelegate)
            await OnCountsChanged.InvokeAsync(newCount);
    }

    private void OpenDetail(JobApplicationModel app)
    {
        detailApp = app;
        detailStatus = app.Status;
        detailNotes = app.Notes ?? string.Empty;
        detailError = string.Empty;
        actionError = null;
    }

    private void CloseDetail() => detailApp = null;

    private async Task SaveDetail()
    {
        if (detailApp is null) return;

        isSavingDetail = true;
        detailError = string.Empty;
        StateHasChanged();

        var result = await JobPostingService.UpdateApplicationStatus(detailApp.Id, new UpdateJobApplicationStatusModel
        {
            Status = detailStatus,
            Notes = string.IsNullOrWhiteSpace(detailNotes) ? null : detailNotes
        });

        if (result.Success && result.Data != null)
        {
            ReplaceApplication(result.Data);
            detailApp = null;
            ApplyFilters();
            await NotifyCounts();
        }
        else
            detailError = result.Message ?? "Failed to update application";

        isSavingDetail = false;
        StateHasChanged();
    }

    private async Task UpdateStatus(JobApplicationModel app, string status)
    {
        actionError = null;
        StateHasChanged();

        var result = await JobPostingService.UpdateApplicationStatus(app.Id, new UpdateJobApplicationStatusModel
        {
            Status = status,
            Notes = app.Notes
        });

        if (result.Success && result.Data != null)
        {
            ReplaceApplication(result.Data);
            ApplyFilters();
            await NotifyCounts();
        }
        else
            actionError = result.Message ?? $"Failed to update application status for {app.Name}.";

        StateHasChanged();
    }

    private void ReplaceApplication(JobApplicationModel updated)
    {
        var index = allItems.FindIndex(x => x.Application.Id == updated.Id);
        if (index >= 0)
        {
            var title = allItems[index].JobTitle;
            allItems[index] = new InboxItem(updated, title);
        }
    }

    private string GetJobTitle(Guid jobId) =>
        jobs.FirstOrDefault(j => j.Id == jobId)?.Title ?? "Unknown role";

    private static string GetPortfolioLabel(string url)
    {
        if (IsSafeHttpUrl(url, out var uri))
            return uri!.Host;
        return url;
    }

    private static bool IsSafeHttpUrl(string? url, out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed))
            return false;

        if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
            return false;

        uri = parsed;
        return true;
    }

    private static string GetStatusBadgeClass(string status) => status switch
    {
        "new" => "badge--new",
        "reviewing" => "badge--reviewing",
        "interviewing" => "badge--interviewing",
        "rejected" => "badge--rejected",
        "hired" => "badge--hired",
        _ => "badge--new"
    };

    private static string GetStatusLabel(string status) => status switch
    {
        "new" => "New",
        "reviewing" => "Reviewed",
        "interviewing" => "Interview",
        "rejected" => "Rejected",
        "hired" => "Hired",
        _ => status
    };

    private static string FormatDate(DateTime? dt)
    {
        if (dt is null) return "—";
        return dt.Value.ToLocalTime().ToString("MMM d, yyyy");
    }
}
