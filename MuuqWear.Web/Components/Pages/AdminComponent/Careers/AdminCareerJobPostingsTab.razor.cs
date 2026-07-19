using Microsoft.AspNetCore.Components;
using MuuqWear.Application.Services.JobPostingService;
using MuuqWear.Model.JobPosting;

namespace MuuqWear.Web.Components.Pages.AdminComponent.Careers;

public partial class AdminCareerJobPostingsTab
{
    [Parameter] public EventCallback<Guid> OnViewApplications { get; set; }

    [Inject] private IJobPostingService JobPostingService { get; set; } = default!;

    private List<JobPostingModel> jobs = [];
    private bool isLoading;
    private bool isFormOpen;
    private bool isEditMode;
    private bool isSaving;
    private bool slugManuallyEdited;
    private string formError = string.Empty;
    private CreateJobPostingModel form = new();
    private JobPostingModel? editingJob;
    private JobPostingModel? deletingJob;

    protected override async Task OnInitializedAsync() => await LoadJobs();

    private async Task LoadJobs()
    {
        isLoading = true;
        StateHasChanged();

        var result = await JobPostingService.GetAll();
        jobs = result.Success && result.Data != null ? result.Data : [];

        isLoading = false;
        StateHasChanged();
    }

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
        slugManuallyEdited = true;
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
        if (!slugManuallyEdited)
            form.Slug = GenerateSlug(form.Title);
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
            formError = result.Message ?? "Failed to create job posting";

        isSaving = false;
        StateHasChanged();
    }

    private async Task HandleEdit()
    {
        if (!ValidateForm() || editingJob is null) return;

        isSaving = true;
        StateHasChanged();

        var result = await JobPostingService.Update(editingJob.Id, new UpdateJobPostingModel
        {
            Slug = form.Slug,
            Title = form.Title,
            Department = form.Department,
            Location = form.Location,
            Type = form.Type,
            Description = form.Description
        });

        if (result.Success && result.Data != null)
        {
            var index = jobs.FindIndex(x => x.Id == editingJob.Id);
            if (index >= 0) jobs[index] = result.Data;
            isFormOpen = false;
        }
        else
            formError = result.Message ?? "Failed to update job posting";

        isSaving = false;
        StateHasChanged();
    }

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
        slugManuallyEdited = true;
        isFormOpen = true;
    }

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

    private void OpenDeleteConfirm(JobPostingModel job) => deletingJob = job;
    private void CancelDelete() => deletingJob = null;

    private async Task ConfirmDelete()
    {
        if (deletingJob is null) return;

        var result = await JobPostingService.Delete(deletingJob.Id);
        if (result.Success)
            jobs.RemoveAll(j => j.Id == deletingJob.Id);

        deletingJob = null;
        StateHasChanged();
    }

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

    private async Task ViewApplications(Guid jobId)
    {
        if (OnViewApplications.HasDelegate)
            await OnViewApplications.InvokeAsync(jobId);
    }
}
