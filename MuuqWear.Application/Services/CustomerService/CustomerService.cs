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
        string? search, int page, int pageSize)
    {
        try
        {
            var url = $"api/Customer?page={page}&pageSize={pageSize}";

            if (!string.IsNullOrEmpty(search))
                url += $"&search={Uri.EscapeDataString(search)}";

            var result = await _http.GetAsync(url);
            return await HttpResponseReader.ReadAsync<PaginatedResponse<CustomerModel>>(result);
        }
        catch (Exception ex)
        {
            return Response<PaginatedResponse<CustomerModel>>.Fail(HttpResponseReader.FromException(ex));
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
}
