using MuuqWear.Application.Services.JobPostingService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.JobApplication;
using MuuqWear.Model.JobPosting;

namespace MuuqWear.Web.Services;

public sealed class FirestoreJobPostingService : IJobPostingService
{
    private readonly AdminFirestore _store;

    public FirestoreJobPostingService(AdminFirestore store) => _store = store;

    public async Task<Response<List<JobPostingModel>>> GetAll() =>
        Response<List<JobPostingModel>>.SuccessResponse(await _store.ListJobPostingsAsync());

    public async Task<Response<List<JobPostingModel>>> GetOpen() =>
        Response<List<JobPostingModel>>.SuccessResponse(
            (await _store.ListJobPostingsAsync()).Where(j => j.IsOpen).ToList());

    public async Task<Response<JobPostingModel>> GetById(Guid id)
    {
        var job = (await _store.ListJobPostingsAsync()).FirstOrDefault(j => j.Id == id);
        return job is null
            ? Response<JobPostingModel>.Fail("Job not found")
            : Response<JobPostingModel>.SuccessResponse(job);
    }

    public async Task<Response<JobPostingModel>> Create(CreateJobPostingModel request) =>
        Response<JobPostingModel>.SuccessResponse(await _store.UpsertJobPostingAsync(new JobPostingModel
        {
            Title = request.Title,
            Slug = request.Slug,
            Department = request.Department,
            Location = request.Location,
            Type = request.Type,
            Description = request.Description,
            Status = "open",
            OpenedAt = DateTime.UtcNow
        }));

    public async Task<Response<JobPostingModel>> Update(Guid id, UpdateJobPostingModel request)
    {
        var existing = (await _store.ListJobPostingsAsync()).FirstOrDefault(j => j.Id == id);
        if (existing is null)
            return Response<JobPostingModel>.Fail("Job not found");
        existing.Title = request.Title;
        existing.Slug = request.Slug;
        existing.Department = request.Department;
        existing.Location = request.Location;
        existing.Type = request.Type;
        existing.Description = request.Description;
        return Response<JobPostingModel>.SuccessResponse(await _store.UpsertJobPostingAsync(existing));
    }

    public async Task<Response<bool>> Delete(Guid id)
    {
        await _store.DeleteJobPostingAsync(id);
        return Response<bool>.SuccessResponse(true);
    }

    public Task<Response<JobPostingModel>> Close(Guid id) => SetStatus(id, "closed");

    public Task<Response<JobPostingModel>> Reopen(Guid id) => SetStatus(id, "open");

    public async Task<Response<JobApplicationModel>> SubmitApplication(Guid jobId, SubmitJobApplicationModel request)
    {
        var app = await _store.UpsertJobApplicationAsync(new JobApplicationModel
        {
            JobId = jobId,
            Name = request.Name,
            Email = request.Email,
            PortfolioUrl = request.PortfolioUrl,
            ResumeUrl = request.ResumeUrl,
            Status = "new"
        });
        return Response<JobApplicationModel>.SuccessResponse(app);
    }

    public async Task<Response<List<JobApplicationModel>>> GetApplicationsByJob(Guid jobId) =>
        Response<List<JobApplicationModel>>.SuccessResponse(await _store.ListJobApplicationsAsync(jobId));

    public async Task<Response<JobApplicationModel>> GetApplicationById(Guid applicationId)
    {
        var app = (await _store.ListJobApplicationsAsync(null)).FirstOrDefault(a => a.Id == applicationId);
        return app is null
            ? Response<JobApplicationModel>.Fail("Application not found")
            : Response<JobApplicationModel>.SuccessResponse(app);
    }

    public async Task<Response<JobApplicationModel>> UpdateApplicationStatus(
        Guid applicationId, UpdateJobApplicationStatusModel request)
    {
        var app = (await _store.ListJobApplicationsAsync(null)).FirstOrDefault(a => a.Id == applicationId);
        if (app is null)
            return Response<JobApplicationModel>.Fail("Application not found");
        app.Status = request.Status;
        app.Notes = request.Notes ?? app.Notes;
        return Response<JobApplicationModel>.SuccessResponse(await _store.UpsertJobApplicationAsync(app));
    }

    public async Task<Response<bool>> DeleteApplication(Guid applicationId)
    {
        await _store.DeleteJobApplicationAsync(applicationId);
        return Response<bool>.SuccessResponse(true);
    }

    public Task<Response<string>> UploadResume(string fileName, byte[] bytes, string contentType) =>
        Task.FromResult(Response<string>.Fail("Resume upload is not available in admin Firestore yet."));

    private async Task<Response<JobPostingModel>> SetStatus(Guid id, string status)
    {
        var existing = (await _store.ListJobPostingsAsync()).FirstOrDefault(j => j.Id == id);
        if (existing is null)
            return Response<JobPostingModel>.Fail("Job not found");
        existing.Status = status;
        existing.ClosedAt = status == "closed" ? DateTime.UtcNow : null;
        existing.OpenedAt = status == "open" ? DateTime.UtcNow : existing.OpenedAt;
        return Response<JobPostingModel>.SuccessResponse(await _store.UpsertJobPostingAsync(existing));
    }
}
