using MuuqWear.Application.Shared;
using MuuqWear.Model.HelpCenter;
using MuuqWear.Model.Shared;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.HelpCenterService;

public class HelpCenterService : IHelpCenterService
{
    private readonly HttpClient _http;

    public HelpCenterService(HttpClient http)
    {
        _http = http;
    }

    // =============================================
    // SUBMIT TICKET
    // =============================================
    public async Task<Response<SupportTicketModel>> SubmitTicket(
        SubmitTicketModel request)
    {
        try
        {
            var result = await _http.PostAsJsonAsync(
                "api/Help/ticket", request);

            var response = await result.Content
                .ReadFromJsonAsync<Response<SupportTicketModel>>();

            return response ?? new Response<SupportTicketModel>
            {
                Success = false,
                Message = result.IsSuccessStatusCode
                    ? "Unexpected response from server"
                    : $"Server error: {result.StatusCode}"
            };
        }
        catch (Exception)
        {
            return new Response<SupportTicketModel>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    // =============================================
    // GET ALL TICKETS (ADMIN)
    // =============================================
    public async Task<Response<PaginatedResponse<SupportTicketModel>>> GetAllTickets(
        string? status, int page, int pageSize)
    {
        try
        {
            var url = $"api/Help/admin/tickets?page={page}&pageSize={pageSize}";

            if (!string.IsNullOrEmpty(status))
                url += $"&status={Uri.EscapeDataString(status)}";

            var result = await _http
                .GetFromJsonAsync<Response<PaginatedResponse<SupportTicketModel>>>(url);

            return result ?? new Response<PaginatedResponse<SupportTicketModel>>
            {
                Success = false,
                Message = "Unexpected response from server"
            };
        }
        catch (Exception)
        {
            return new Response<PaginatedResponse<SupportTicketModel>>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    // =============================================
    // GET TICKET BY ID (ADMIN)
    // =============================================
    public async Task<Response<SupportTicketModel>> GetTicketById(Guid ticketId)
    {
        try
        {
            var result = await _http
                .GetFromJsonAsync<Response<SupportTicketModel>>(
                    $"api/Help/admin/tickets/{ticketId}");

            return result ?? new Response<SupportTicketModel>
            {
                Success = false,
                Message = "Unexpected response from server"
            };
        }
        catch (Exception)
        {
            return new Response<SupportTicketModel>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    // =============================================
    // UPDATE TICKET STATUS (ADMIN)
    // =============================================
    public async Task<Response<SupportTicketModel>> UpdateTicketStatus(
        Guid ticketId, string status)
    {
        try
        {
            var result = await _http.PatchAsJsonAsync(
                $"api/Help/admin/tickets/{ticketId}/status",
                new UpdateTicketStatusModel { Status = status });

            var response = await result.Content
                .ReadFromJsonAsync<Response<SupportTicketModel>>();

            return response ?? new Response<SupportTicketModel>
            {
                Success = false,
                Message = result.IsSuccessStatusCode
                    ? "Unexpected response from server"
                    : $"Server error: {result.StatusCode}"
            };
        }
        catch (Exception)
        {
            return new Response<SupportTicketModel>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    // =============================================
    // GET STATS (ADMIN)
    // =============================================
    public async Task<Response<TicketStatsModel>> GetStats()
    {
        try
        {
            var result = await _http
                .GetFromJsonAsync<Response<TicketStatsModel>>(
                    "api/Help/admin/stats");

            return result ?? new Response<TicketStatsModel>
            {
                Success = false,
                Message = "Unexpected response from server"
            };
        }
        catch (Exception)
        {
            return new Response<TicketStatsModel>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }
}
