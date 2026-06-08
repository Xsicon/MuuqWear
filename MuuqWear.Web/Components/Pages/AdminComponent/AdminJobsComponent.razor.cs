using Microsoft.AspNetCore.Components;
using MuuqWear.Model.JobPosting;

namespace MuuqWear.Web.Components.Pages.AdminComponent;
public partial class AdminJobsComponent
{
    private List<JobPostingModel> jobs = new();
    private bool isLoading = false;

    // Form state
    private bool isFormOpen = false;
    private bool isEditMode = false;
    private bool isSaving = false;
    private bool slugManuallyEdited = false;
    private string formError = string.Empty;
    private CreateJobPostingModel form = new();
    private JobPostingModel? editingJob = null;

    // Delete state
    private JobPostingModel? deletingJob = null;

    protected override async Task OnInitializedAsync()
    {
        await LoadJobs();
    }

    private async Task LoadJobs()
    {
        isLoading = true;
        StateHasChanged();

        var result = await JobPostingService.GetAll();
        jobs = result.Success && result.Data != null
            ? result.Data
            : new();

        isLoading = false;
        StateHasChanged();
    }

    // =============================================
    // FORM — OPEN, CLOSE, SLUG-FROM-TITLE
    // =============================================

    private void OpenCreateForm()
    {
        isEditMode = false;
        editingJob = null;
        form = new CreateJobPostingModel();
        formError = string.Empty;
        slugManuallyEdited = false;
        isFormOpen = true;
    }

    private void OpenEditForm(JobPostingModel job)
    {
        isEditMode = true;
        editingJob = job;
        form = new CreateJobPostingModel
        {
            Slug = job.Slug,
            Title = job.Title,
            Department = job.Department,
            Location = job.Location,
            Type = job.Type,
            Description = job.Description
        };
        formError = string.Empty;
        slugManuallyEdited = true; // existing record — don't auto-replace
        isFormOpen = true;
    }

    private void CloseForm()
    {
        isFormOpen = false;
        isEditMode = false;
        editingJob = null;
    }

    private void OnTitleChanged(ChangeEventArgs e)
    {
        form.Title = e.Value?.ToString() ?? string.Empty;

        // Only auto-generate slug if user hasn't manually edited it
        if (!slugManuallyEdited)
        {
            form.Slug = GenerateSlug(form.Title);
        }
    }

    private static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        return title
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace(":", "")
            .Replace(";", "")
            .Replace("?", "")
            .Replace("!", "")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace("(", "")
            .Replace(")", "");
    }

    // =============================================
    // CREATE
    // =============================================

    private async Task HandleCreate()
    {
        if (!ValidateForm()) return;

        isSaving = true;
        StateHasChanged();

        var result = await JobPostingService.Create(form);

        if (result.Success && result.Data != null)
        {
            jobs.Insert(0, result.Data);
            isFormOpen = false;
        }
        else
        {
            formError = result.Message ?? "Failed to create job posting";
        }

        isSaving = false;
        StateHasChanged();
    }

    // =============================================
    // EDIT
    // =============================================

    private async Task HandleEdit()
    {
        if (!ValidateForm()) return;
        if (editingJob is null) return;

        isSaving = true;
        StateHasChanged();

        var updateRequest = new UpdateJobPostingModel
        {
            Slug = form.Slug,
            Title = form.Title,
            Department = form.Department,
            Location = form.Location,
            Type = form.Type,
            Description = form.Description
        };

        var result = await JobPostingService.Update(editingJob.Id, updateRequest);

        if (result.Success && result.Data != null)
        {
            var index = jobs.FindIndex(x => x.Id == editingJob.Id);
            if (index >= 0) jobs[index] = result.Data;
            isFormOpen = false;
        }
        else
        {
            formError = result.Message ?? "Failed to update job posting";
        }

        isSaving = false;
        StateHasChanged();
    }

    // =============================================
    // DUPLICATE
    // =============================================

    private void DuplicateJob(JobPostingModel job)
    {
        isEditMode = false;
        editingJob = null;

        form = new CreateJobPostingModel
        {
            Slug = $"{job.Slug}-copy",
            Title = $"{job.Title} (Copy)",
            Department = job.Department,
            Location = job.Location,
            Type = job.Type,
            Description = job.Description
        };

        formError = string.Empty;
        slugManuallyEdited = true; // pre-filled, don't overwrite
        isFormOpen = true;
    }

    // =============================================
    // CLOSE / REOPEN
    // =============================================

    private async Task HandleClose(Guid id)
    {
        var result = await JobPostingService.Close(id);
        if (result.Success && result.Data != null)
        {
            var index = jobs.FindIndex(x => x.Id == id);
            if (index >= 0) jobs[index] = result.Data;
            StateHasChanged();
        }
    }

    private async Task HandleReopen(Guid id)
    {
        var result = await JobPostingService.Reopen(id);
        if (result.Success && result.Data != null)
        {
            var index = jobs.FindIndex(x => x.Id == id);
            if (index >= 0) jobs[index] = result.Data;
            StateHasChanged();
        }
    }

    // =============================================
    // DELETE
    // =============================================

    private void OpenDeleteConfirm(JobPostingModel job)
    {
        deletingJob = job;
    }

    private void CancelDelete()
    {
        deletingJob = null;
    }

    private async Task ConfirmDelete()
    {
        if (deletingJob is null) return;

        var result = await JobPostingService.Delete(deletingJob.Id);

        if (result.Success)
        {
            jobs.RemoveAll(j => j.Id == deletingJob.Id);
        }

        deletingJob = null;
        StateHasChanged();
    }

    // =============================================
    // VALIDATION
    // =============================================

    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(form.Title))
        { formError = "Title is required"; return false; }
        if (string.IsNullOrWhiteSpace(form.Slug))
        { formError = "Slug is required"; return false; }
        if (string.IsNullOrWhiteSpace(form.Department))
        { formError = "Department is required"; return false; }
        if (string.IsNullOrWhiteSpace(form.Location))
        { formError = "Location is required"; return false; }
        if (string.IsNullOrWhiteSpace(form.Type))
        { formError = "Type is required"; return false; }

        formError = string.Empty;
        return true;
    }

    private void ViewApplications(Guid jobId)
    {
        NavigationManager.NavigateTo($"/admin/jobs/{jobId}/applications");
    }
}
