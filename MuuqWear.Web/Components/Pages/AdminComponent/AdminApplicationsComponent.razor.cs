using Microsoft.AspNetCore.Components;
using MuuqWear.Model.JobApplication;
using MuuqWear.Model.JobPosting;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminApplicationsComponent
{
    [Parameter] public Guid JobId { get; set; }

    private JobPostingModel? job = null;
    private List<JobApplicationModel> applications = new();
    private List<JobApplicationModel> filteredApplications = new();
    private bool isLoading = false;

    // Detail panel state
    private JobApplicationModel? detailApp = null;
    private string detailStatus = "new";
    private string detailNotes = string.Empty;
    private bool isSavingDetail = false;
    private string detailError = string.Empty;

    // Delete state
    private JobApplicationModel? deletingApp = null;

    // Filter state
    private string statusFilter = "all";

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        isLoading = true;
        StateHasChanged();

        // Load job details
        var jobResult = await JobPostingService.GetById(JobId);
        job = jobResult.Success ? jobResult.Data : null;

        // Load applications for this job
        var appsResult = await JobPostingService.GetApplicationsByJob(JobId);
        applications = appsResult.Success && appsResult.Data != null
            ? appsResult.Data
            : new();

        ApplyFilter();

        isLoading = false;
        StateHasChanged();
    }

    private void ApplyFilter()
    {
        filteredApplications = statusFilter == "all"
            ? applications
            : applications.Where(a => a.Status == statusFilter).ToList();
    }

    // =============================================
    // DETAIL PANEL — OPEN, CLOSE, SAVE
    // =============================================

    private void OpenDetail(JobApplicationModel app)
    {
        detailApp = app;
        detailStatus = app.Status;
        detailNotes = app.Notes ?? string.Empty;
        detailError = string.Empty;
    }

    private void CloseDetail()
    {
        detailApp = null;
    }

    private async Task SaveDetail()
    {
        if (detailApp is null) return;

        isSavingDetail = true;
        StateHasChanged();

        var request = new UpdateJobApplicationStatusModel
        {
            Status = detailStatus,
            Notes = string.IsNullOrWhiteSpace(detailNotes) ? null : detailNotes
        };

        var result = await JobPostingService.UpdateApplicationStatus(
            detailApp.Id, request);

        if (result.Success && result.Data != null)
        {
            // Update the list in-place
            var index = applications.FindIndex(a => a.Id == detailApp.Id);
            if (index >= 0) applications[index] = result.Data;

            ApplyFilter();
            detailApp = null;
        }
        else
        {
            detailError = result.Message ?? "Failed to update application";
        }

        isSavingDetail = false;
        StateHasChanged();
    }

    // =============================================
    // DELETE
    // =============================================

    private void OpenDeleteConfirm()
    {
        deletingApp = detailApp;
        detailApp = null; // close the detail panel
    }

    private void CancelDelete()
    {
        deletingApp = null;
    }

    private async Task ConfirmDelete()
    {
        if (deletingApp is null) return;

        var result = await JobPostingService.DeleteApplication(deletingApp.Id);

        if (result.Success)
        {
            applications.RemoveAll(a => a.Id == deletingApp.Id);
            ApplyFilter();
        }

        deletingApp = null;
        StateHasChanged();
    }

    // =============================================
    // NAVIGATION
    // =============================================

    private void GoBack()
    {
        Navigation.NavigateTo("/admin/jobs");
    }

    // =============================================
    // HELPERS
    // =============================================

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
        "reviewing" => "Reviewing",
        "interviewing" => "Interviewing",
        "rejected" => "Rejected",
        "hired" => "Hired",
        _ => status
    };

    private static string FormatDate(DateTime? dt)
    {
        if (dt is null) return "—";
        var d = dt.Value;
        var now = DateTime.UtcNow;
        var diff = now - d;

        if (diff.TotalDays < 1)
            return d.ToString("h:mm tt") + " today";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays} day{((int)diff.TotalDays == 1 ? "" : "s")} ago";
        return d.ToString("MMM d, yyyy");
    }
}
