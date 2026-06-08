using MuuqWear.Application.Shared;
using MuuqWear.Model.JobApplication;
using MuuqWear.Model.JobPosting;

namespace MuuqWear.Application.Services.JobPostingService;

public interface IJobPostingService
{
    Task<Response<List<JobPostingModel>>> GetAll();
    Task<Response<List<JobPostingModel>>> GetOpen();
    Task<Response<JobPostingModel>> GetById(Guid id);
    Task<Response<JobPostingModel>> Create(CreateJobPostingModel request);
    Task<Response<JobPostingModel>> Update(Guid id, UpdateJobPostingModel request);
    Task<Response<bool>> Delete(Guid id);
    Task<Response<JobPostingModel>> Close(Guid id);
    Task<Response<JobPostingModel>> Reopen(Guid id);

    //Job Appliation Methods

    Task<Response<JobApplicationModel>> SubmitApplication(
       Guid jobId, SubmitJobApplicationModel request);
    Task<Response<List<JobApplicationModel>>> GetApplicationsByJob(Guid jobId);
    Task<Response<JobApplicationModel>> GetApplicationById(Guid applicationId);
    Task<Response<JobApplicationModel>> UpdateApplicationStatus(
        Guid applicationId, UpdateJobApplicationStatusModel request);
    Task<Response<bool>> DeleteApplication(Guid applicationId);

    // ─────────────────────────────────────────────
    // Resume upload (new)
    // ─────────────────────────────────────────────
    Task<Response<string>> UploadResume(
        string fileName, byte[] bytes, string contentType);
}
