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
            return await ReadResponse<PaginatedResponse<CustomerModel>>(result);
        }
        catch (Exception ex)
        {
            return Response<PaginatedResponse<CustomerModel>>.Fail(ex.Message);
        }
    }

    public async Task<Response<List<CustomerNoteModel>>> GetNotes(Guid customerId)
    {
        try
        {
            var result = await _http.GetAsync($"api/Customer/{customerId}/notes");
            return await ReadResponse<List<CustomerNoteModel>>(result);
        }
        catch (Exception ex)
        {
            return Response<List<CustomerNoteModel>>.Fail(ex.Message);
        }
    }

    public async Task<Response<CustomerNoteModel>> AddNote(
        Guid customerId, CreateCustomerNoteModel request)
    {
        try
        {
            var result = await _http.PostAsJsonAsync(
                $"api/Customer/{customerId}/notes", request);
            return await ReadResponse<CustomerNoteModel>(result);
        }
        catch (Exception ex)
        {
            return Response<CustomerNoteModel>.Fail(ex.Message);
        }
    }

    private static async Task<Response<T>> ReadResponse<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(message))
                message = $"Server error: {response.StatusCode}";

            return Response<T>.Fail(message);
        }

        try
        {
            var result = await response.Content.ReadFromJsonAsync<Response<T>>();
            return result ?? Response<T>.Fail("Empty response");
        }
        catch
        {
            return Response<T>.Fail("Failed to parse response");
        }
    }
}
