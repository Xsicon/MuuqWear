using MuuqWear.Application.Shared;
using MuuqWear.Model.JobApplication;
using MuuqWear.Model.JobPosting;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.JobPostingService;

public class JobPostingService : IJobPostingService
{
    private readonly HttpClient _http;

    public JobPostingService(HttpClient http)
    {
        _http = http;
    }

    // =============================================
    // GET ALL — admin: returns open + closed
    // =============================================
    public async Task<Response<List<JobPostingModel>>> GetAll()
    {
        try
        {
            var result = await _http.GetAsync("api/JobPosting");
            return await HttpResponseReader.ReadAsync<List<JobPostingModel>>(result);
        }
        catch (Exception ex)
        {
            return Response<List<JobPostingModel>>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    // =============================================
    // GET OPEN — public Career page
    // =============================================
    public async Task<Response<List<JobPostingModel>>> GetOpen()
    {
        try
        {
            var result = await _http.GetAsync("api/JobPosting/open");
            return await HttpResponseReader.ReadAsync<List<JobPostingModel>>(result);
        }
        catch (Exception ex)
        {
            return Response<List<JobPostingModel>>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    // =============================================
    // GET BY ID
    // =============================================
    public async Task<Response<JobPostingModel>> GetById(Guid id)
    {
        try
        {
            var result = await _http.GetAsync($"api/JobPosting/{id}");
            return await HttpResponseReader.ReadAsync<JobPostingModel>(result);
        }
        catch (Exception ex)
        {
            return Response<JobPostingModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    // =============================================
    // CREATE
    // =============================================
    public async Task<Response<JobPostingModel>> Create(CreateJobPostingModel request)
    {
        try
        {
            var result = await _http.PostAsJsonAsync("api/JobPosting", request);
            return await HttpResponseReader.ReadAsync<JobPostingModel>(result);
        }
        catch (Exception ex)
        {
            return Response<JobPostingModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    // =============================================
    // UPDATE
    // =============================================
    public async Task<Response<JobPostingModel>> Update(
        Guid id, UpdateJobPostingModel request)
    {
        try
        {
            var result = await _http.PutAsJsonAsync($"api/JobPosting/{id}", request);
            return await HttpResponseReader.ReadAsync<JobPostingModel>(result);
        }
        catch (Exception ex)
        {
            return Response<JobPostingModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    // =============================================
    // DELETE
    // =============================================
    public async Task<Response<bool>> Delete(Guid id)
    {
        try
        {
            var result = await _http.DeleteAsync($"api/JobPosting/{id}");
            return await HttpResponseReader.ReadAsync<bool>(result);
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    // =============================================
    // CLOSE
    // =============================================
    public async Task<Response<JobPostingModel>> Close(Guid id)
    {
        try
        {
            var result = await _http.PatchAsync(
                $"api/JobPosting/{id}/close", null);
            return await HttpResponseReader.ReadAsync<JobPostingModel>(result);
        }
        catch (Exception ex)
        {
            return Response<JobPostingModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    // =============================================
    // REOPEN
    // =============================================
    public async Task<Response<JobPostingModel>> Reopen(Guid id)
    {
        try
        {
            var result = await _http.PatchAsync(
                $"api/JobPosting/{id}/reopen", null);
            return await HttpResponseReader.ReadAsync<JobPostingModel>(result);
        }
        catch (Exception ex)
        {
            return Response<JobPostingModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    // =============================================
    // SUBMIT APPLICATION — public
    // =============================================
    public async Task<Response<JobApplicationModel>> SubmitApplication(
        Guid jobId, SubmitJobApplicationModel request)
    {
        try
        {
            var result = await _http.PostAsJsonAsync(
                $"api/JobPosting/{jobId}/applications", request);
            return await HttpResponseReader.ReadAsync<JobApplicationModel>(result);
        }
        catch (Exception ex)
        {
            return Response<JobApplicationModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    // =============================================
    // GET APPLICATIONS FOR A JOB — admin
    // =============================================
    public async Task<Response<List<JobApplicationModel>>> GetApplicationsByJob(Guid jobId)
    {
        try
        {
            var result = await _http.GetAsync(
                $"api/JobPosting/{jobId}/applications");
            return await HttpResponseReader.ReadAsync<List<JobApplicationModel>>(result);
        }
        catch (Exception ex)
        {
            return Response<List<JobApplicationModel>>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    // =============================================
    // GET APPLICATION BY ID — admin
    // =============================================
    public async Task<Response<JobApplicationModel>> GetApplicationById(Guid applicationId)
    {
        try
        {
            var result = await _http.GetAsync(
                $"api/JobPosting/applications/{applicationId}");
            return await HttpResponseReader.ReadAsync<JobApplicationModel>(result);
        }
        catch (Exception ex)
        {
            return Response<JobApplicationModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    // =============================================
    // UPDATE APPLICATION STATUS / NOTES — admin
    // =============================================
    public async Task<Response<JobApplicationModel>> UpdateApplicationStatus(
        Guid applicationId, UpdateJobApplicationStatusModel request)
    {
        try
        {
            var result = await _http.PatchAsJsonAsync(
                $"api/JobPosting/applications/{applicationId}/status", request);
            return await HttpResponseReader.ReadAsync<JobApplicationModel>(result);
        }
        catch (Exception ex)
        {
            return Response<JobApplicationModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    // =============================================
    // DELETE APPLICATION — admin
    // =============================================
    public async Task<Response<bool>> DeleteApplication(Guid applicationId)
    {
        try
        {
            var result = await _http.DeleteAsync(
                $"api/JobPosting/applications/{applicationId}");
            return await HttpResponseReader.ReadAsync<bool>(result);
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    // =============================================
    // UPLOAD RESUME
    // =============================================
    public async Task<Response<string>> UploadResume(
        string fileName, byte[] bytes, string contentType)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName);

            var result = await _http.PostAsync(
                "api/JobPosting/applications/upload-resume", content);
            return await HttpResponseReader.ReadAsync<string>(result);
        }
        catch (Exception ex)
        {
            return Response<string>.Fail(HttpResponseReader.FromException(ex));
        }
    }
}
