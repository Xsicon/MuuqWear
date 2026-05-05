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

    // =============================================
    // GET ALL CUSTOMERS
    // =============================================
    public async Task<Response<PaginatedResponse<CustomerModel>>> GetAll(
        string? search, int page, int pageSize)
    {
        try
        {
            var url = $"api/Customer?page={page}&pageSize={pageSize}";

            if (!string.IsNullOrEmpty(search))
                url += $"&search={Uri.EscapeDataString(search)}";

            var result = await _http.GetAsync(url);

            if (!result.IsSuccessStatusCode)
                return new Response<PaginatedResponse<CustomerModel>>
                {
                    Success = false,
                    Message = $"Server error: {result.StatusCode}"
                };

            var response = await result.Content
                .ReadFromJsonAsync<Response<PaginatedResponse<CustomerModel>>>();

            return response ?? new Response<PaginatedResponse<CustomerModel>>
            {
                Success = false,
                Message = "Empty response"
            };
        }
        catch (Exception ex)
        {
            return new Response<PaginatedResponse<CustomerModel>>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
}