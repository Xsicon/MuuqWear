using MuuqWear.Application.Shared;
using MuuqWear.Model.Refund;
using MuuqWear.Model.Shared;
using System.Net.Http.Json;
using System.Text.Json;

namespace MuuqWear.Application.Services.RefundService;

public class RefundService : IRefundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;

    public RefundService(HttpClient http)
    {
        _http = http;
    }

    public async Task<Response<PaginatedResponse<RefundModel>>> GetAllRefunds(
        string? status, int page, int pageSize)
    {
        try
        {
            var url = $"api/Refund/admin?page={page}&pageSize={pageSize}";

            if (!string.IsNullOrEmpty(status))
                url += $"&status={Uri.EscapeDataString(status)}";

            var httpResponse = await _http.GetAsync(url);
            return await ReadPaginatedResponseAsync(httpResponse);
        }
        catch (Exception ex)
        {
            return new Response<PaginatedResponse<RefundModel>>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<Response<RefundModel>> ProcessRefund(Guid refundId)
    {
        try
        {
            var httpResponse = await _http.PostAsync(
                $"api/Refund/admin/{refundId}/process", null);

            return await ReadResponseAsync(httpResponse);
        }
        catch (Exception ex)
        {
            return new Response<RefundModel>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    private static async Task<Response<PaginatedResponse<RefundModel>>>
        ReadPaginatedResponseAsync(HttpResponseMessage httpResponse)
    {
        var body = await httpResponse.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(body))
        {
            return new Response<PaginatedResponse<RefundModel>>
            {
                Success = false,
                Message = $"Empty response from server (HTTP {(int)httpResponse.StatusCode})"
            };
        }

        try
        {
            var response = JsonSerializer
                .Deserialize<Response<PaginatedResponse<RefundModel>>>(body, JsonOptions);

            if (response != null)
                return response;
        }
        catch
        {
            // fall through
        }

        return new Response<PaginatedResponse<RefundModel>>
        {
            Success = false,
            Message = $"Unexpected response (HTTP {(int)httpResponse.StatusCode}): {body}"
        };
    }

    private static async Task<Response<RefundModel>> ReadResponseAsync(
        HttpResponseMessage httpResponse)
    {
        var body = await httpResponse.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(body))
        {
            return new Response<RefundModel>
            {
                Success = false,
                Message = $"Empty response from server (HTTP {(int)httpResponse.StatusCode})"
            };
        }

        try
        {
            var response = JsonSerializer
                .Deserialize<Response<RefundModel>>(body, JsonOptions);

            if (response != null)
                return response;
        }
        catch
        {
            // fall through
        }

        return new Response<RefundModel>
        {
            Success = false,
            Message = $"Unexpected response (HTTP {(int)httpResponse.StatusCode}): {body}"
        };
    }
}
