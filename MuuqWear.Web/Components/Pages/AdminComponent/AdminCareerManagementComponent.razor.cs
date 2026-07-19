using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using MuuqWear.Application.Services.JobPostingService;
using MuuqWear.Application.Shared;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminCareerManagementComponent : IDisposable
{
    private static readonly (string Id, string Label)[] TabViews =
    [
        ("postings", "Job Postings"),
        ("applications", "Applications Inbox"),
        ("settings", "Career Page Settings")
    ];

    [Inject] private IJobPostingService JobPostingService { get; set; } = default!;

    private string activeTab = "postings";
    private int newApplications;
    private Guid? applicationsJobFilter;

    protected override async Task OnInitializedAsync()
    {
        CareersTabCoordinator.TabChanged += OnCareersTabChanged;
        NavigationManager.LocationChanged += OnLocationChanged;
        ApplyTabFromUri();
        await RefreshNewApplicationCountAsync();
    }

    private void OnCareersTabChanged(string tab)
    {
        var normalized = AdminCareersTabCoordinator.NormalizeTab(tab);
        if (activeTab == normalized)
            return;

        activeTab = normalized;
        InvokeAsync(StateHasChanged);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) =>
        InvokeAsync(async () =>
        {
            ApplyTabFromUri();
            await RefreshNewApplicationCountAsync();
            StateHasChanged();
        });

    private void ApplyTabFromUri()
    {
        var path = NavigationManager.ToBaseRelativePath(NavigationManager.Uri).Trim('/').ToLowerInvariant();
        if (!path.StartsWith("admin/careers", StringComparison.Ordinal)
            && !path.StartsWith("admin/jobs", StringComparison.Ordinal))
            return;

        if (path.Contains("/applications", StringComparison.Ordinal))
        {
            activeTab = "applications";
        }
        else
        {
            var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
            var query = QueryHelpers.ParseQuery(uri.Query);
            activeTab = query.TryGetValue("tab", out var tab)
                ? AdminCareersTabCoordinator.NormalizeTab(tab)
                : "postings";
        }

        applicationsJobFilter = null;
        var uriForJob = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var queryForJob = QueryHelpers.ParseQuery(uriForJob.Query);
        if (queryForJob.TryGetValue("jobId", out var jobIdValue)
            && Guid.TryParse(jobIdValue, out var jobId))
            applicationsJobFilter = jobId;
    }

    private void SwitchTab(string tab)
    {
        var normalized = AdminCareersTabCoordinator.NormalizeTab(tab);
        if (activeTab == normalized && normalized != "applications")
            return;

        activeTab = normalized;
        applicationsJobFilter = null;
        NavigationManager.NavigateTo($"/admin/careers?tab={normalized}");
        CareersTabCoordinator.NotifyTabChanged(normalized);
    }

    private void ViewApplicationsForJob(Guid jobId)
    {
        activeTab = "applications";
        applicationsJobFilter = jobId;
        NavigationManager.NavigateTo($"/admin/careers?tab=applications&jobId={jobId}");
        CareersTabCoordinator.NotifyTabChanged("applications");
    }

    private Task HandleApplicationCounts(int newCount)
    {
        newApplications = newCount;
        return InvokeAsync(StateHasChanged);
    }

    private async Task RefreshNewApplicationCountAsync()
    {
        var jobsResult = await JobPostingService.GetAll();
        if (!jobsResult.Success || jobsResult.Data is null || jobsResult.Data.Count == 0)
        {
            newApplications = 0;
            return;
        }

        var tasks = jobsResult.Data.Select(async job =>
        {
            var appsResult = await JobPostingService.GetApplicationsByJob(job.Id);
            return appsResult.Success && appsResult.Data != null ? appsResult.Data : [];
        });

        var results = await Task.WhenAll(tasks);
        newApplications = results.SelectMany(x => x).Count(app => app.Status == "new");
    }

    public void Dispose()
    {
        CareersTabCoordinator.TabChanged -= OnCareersTabChanged;
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
