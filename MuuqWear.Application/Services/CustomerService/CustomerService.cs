using MuuqWear.Application.Shared;
using MuuqWear.Model.Customer;
using MuuqWear.Model.Shared;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.CustomerService;

public class CustomerService : ICustomerService
{
    private readonly HttpClient _http;

    public CustomerService(HttpClient http)
    {
        _http = http;
    }

    public async Task<Response<PaginatedResponse<CustomerModel>>> GetAll(
        string? search, int page, int pageSize, string? status = null)
    {
        try
        {
            var url = $"api/Customer?page={page}&pageSize={pageSize}";

            if (!string.IsNullOrEmpty(search))
                url += $"&search={Uri.EscapeDataString(search)}";

            if (!string.IsNullOrEmpty(status))
                url += $"&status={Uri.EscapeDataString(status)}";

            var result = await _http.GetAsync(url);
            return await HttpResponseReader.ReadAsync<PaginatedResponse<CustomerModel>>(result);
        }
        catch (Exception ex)
        {
            return Response<PaginatedResponse<CustomerModel>>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    public async Task<Response<CustomerModel>> GetById(Guid customerId)
    {
        try
        {
            var result = await _http.GetAsync($"api/Customer/{customerId}");
            return await HttpResponseReader.ReadAsync<CustomerModel>(result);
        }
        catch (Exception ex)
        {
            return Response<CustomerModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    public async Task<Response<List<CustomerNoteModel>>> GetNotes(Guid customerId)
    {
        try
        {
            var result = await _http.GetAsync($"api/Customer/{customerId}/notes");
            return await HttpResponseReader.ReadAsync<List<CustomerNoteModel>>(result);
        }
        catch (Exception ex)
        {
            return Response<List<CustomerNoteModel>>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    public async Task<Response<CustomerNoteModel>> AddNote(
        Guid customerId, CreateCustomerNoteModel request)
    {
        try
        {
            var result = await _http.PostAsJsonAsync(
                $"api/Customer/{customerId}/notes", request);
            return await HttpResponseReader.ReadAsync<CustomerNoteModel>(result);
        }
        catch (Exception ex)
        {
            return Response<CustomerNoteModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    public async Task<Response<CustomerModel>> Suspend(
        Guid customerId, SuspendCustomerModel request)
    {
        try
        {
            var result = await _http.PatchAsJsonAsync(
                $"api/Customer/{customerId}/suspend", request);
            return await HttpResponseReader.ReadAsync<CustomerModel>(result);
        }
        catch (Exception ex)
        {
            return Response<CustomerModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    public async Task<Response<CustomerModel>> Reactivate(
        Guid customerId, ReactivateCustomerModel? request = null)
    {
        try
        {
            var result = await _http.PatchAsJsonAsync(
                $"api/Customer/{customerId}/reactivate", request ?? new ReactivateCustomerModel());
            return await HttpResponseReader.ReadAsync<CustomerModel>(result);
        }
        catch (Exception ex)
        {
            return Response<CustomerModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }
}
