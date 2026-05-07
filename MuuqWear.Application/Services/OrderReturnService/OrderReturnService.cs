using MuuqWear.Application.Shared;
using MuuqWear.Model.OrderReturn;
using MuuqWear.Model.Shared;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.OrderReturnService;

public class OrderReturnService : IOrderReturnService
{
    private readonly HttpClient _http;

    public OrderReturnService(HttpClient http)
    {
        _http = http;
    }

    // =============================================
    // SUBMIT RETURN
    // =============================================
    public async Task<Response<OrderReturnModel>> SubmitReturn(
     SubmitReturnModel request)
    {
        try
        {
            var result = await _http.PostAsJsonAsync(
                "api/Return/submit", request);

            // attempt to deserialize — our backend always returns Response<T>
            // for 200 and 400. For anything else deserialize will return null
            // and we fall back to status code message
            var response = await result.Content
                .ReadFromJsonAsync<Response<OrderReturnModel>>();

            //  if deserialization succeeded and has meaningful data use it
            if (response != null)
                return response;

            //  fallback for unexpected responses (401, 429, 502 etc)
            return new Response<OrderReturnModel>
            {
                Success = false,
                Message = result.IsSuccessStatusCode
                    ? "Unexpected response from server"
                    : $"Server error: {result.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            //  network errors, timeouts, JSON parse failures
            return new Response<OrderReturnModel>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    // =============================================
    // GET ALL RETURNS (ADMIN)
    // =============================================
    public async Task<Response<PaginatedResponse<OrderReturnModel>>> GetAllReturns(
        string? status, int page, int pageSize)
    {
        try
        {
            var url = $"api/Return/admin?page={page}&pageSize={pageSize}";

            if (!string.IsNullOrEmpty(status))
                url += $"&status={Uri.EscapeDataString(status)}";

            var result = await _http
                .GetFromJsonAsync<Response<PaginatedResponse<OrderReturnModel>>>(url);

            return result ?? new Response<PaginatedResponse<OrderReturnModel>>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<PaginatedResponse<OrderReturnModel>>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    // =============================================
    // UPDATE RETURN STATUS (ADMIN)
    // =============================================
    public async Task<Response<OrderReturnModel>> UpdateReturnStatus(
        Guid returnId, string status)
    {
        try
        {
            var result = await _http.PatchAsJsonAsync(
                $"api/Return/admin/{returnId}/status",
                new UpdateReturnStatusModel { Status = status });

            //if (!result.IsSuccessStatusCode)
            //    return new Response<OrderReturnModel>
            //    {
            //        Success = false,
            //        Message = $"Server error: {result.StatusCode}"
            //    };

            var response = await result.Content
                .ReadFromJsonAsync<Response<OrderReturnModel>>();

            return response ?? new Response<OrderReturnModel>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<OrderReturnModel>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
}
