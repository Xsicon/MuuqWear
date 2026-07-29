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

    // =============================================
    // HELP ARTICLES — PUBLIC
    // =============================================
    public async Task<Response<PaginatedResponse<HelpArticleModel>>> GetPublishedArticles(
        string? category, string? search, int page, int pageSize)
    {
        try
        {
            var url = BuildArticlesUrl(
                "api/Help/articles", category, null, search, page, pageSize);

            var result = await _http
                .GetFromJsonAsync<Response<PaginatedResponse<HelpArticleModel>>>(url);

            return NormalizeArticlesResponse(result);
        }
        catch (Exception)
        {
            return ConnectionError<PaginatedResponse<HelpArticleModel>>();
        }
    }

    public async Task<Response<HelpArticleModel>> GetPublishedArticleById(Guid articleId)
    {
        try
        {
            var result = await _http
                .GetFromJsonAsync<Response<HelpArticleModel>>(
                    $"api/Help/articles/{articleId}");

            return NormalizeArticleResponse(result);
        }
        catch (Exception)
        {
            return ConnectionError<HelpArticleModel>();
        }
    }

    // =============================================
    // HELP ARTICLES — ADMIN
    // =============================================
    public async Task<Response<PaginatedResponse<HelpArticleModel>>> GetAdminArticles(
        string? category, string? status, string? search, int page, int pageSize)
    {
        try
        {
            var url = BuildArticlesUrl(
                "api/Help/admin/articles", category, status, search, page, pageSize);

            var result = await _http
                .GetFromJsonAsync<Response<PaginatedResponse<HelpArticleModel>>>(url);

            return NormalizeArticlesResponse(result);
        }
        catch (Exception)
        {
            return ConnectionError<PaginatedResponse<HelpArticleModel>>();
        }
    }

    public async Task<Response<HelpArticleModel>> GetAdminArticleById(Guid articleId)
    {
        try
        {
            var result = await _http
                .GetFromJsonAsync<Response<HelpArticleModel>>(
                    $"api/Help/admin/articles/{articleId}");

            return NormalizeArticleResponse(result);
        }
        catch (Exception)
        {
            return ConnectionError<HelpArticleModel>();
        }
    }

    public async Task<Response<HelpArticleModel>> CreateArticle(SaveHelpArticleModel request)
    {
        try
        {
            var payload = ToSavePayload(request);
            var result = await _http.PostAsJsonAsync("api/Help/admin/articles", payload);
            var response = await result.Content
                .ReadFromJsonAsync<Response<HelpArticleModel>>();

            return NormalizeArticleResponse(response, result.IsSuccessStatusCode, result.StatusCode);
        }
        catch (Exception)
        {
            return ConnectionError<HelpArticleModel>();
        }
    }

    public async Task<Response<HelpArticleModel>> UpdateArticle(
        Guid articleId, SaveHelpArticleModel request)
    {
        try
        {
            var payload = ToSavePayload(request);
            var result = await _http.PutAsJsonAsync(
                $"api/Help/admin/articles/{articleId}", payload);
            var response = await result.Content
                .ReadFromJsonAsync<Response<HelpArticleModel>>();

            return NormalizeArticleResponse(response, result.IsSuccessStatusCode, result.StatusCode);
        }
        catch (Exception)
        {
            return ConnectionError<HelpArticleModel>();
        }
    }

    public async Task<Response<HelpArticleModel>> UpdateArticleStatus(
        Guid articleId, string status)
    {
        try
        {
            var result = await _http.PatchAsJsonAsync(
                $"api/Help/admin/articles/{articleId}/status",
                new UpdateHelpArticleStatusModel
                {
                    Status = HelpArticleDisplayStatus.ToApi(status)
                });

            var response = await result.Content
                .ReadFromJsonAsync<Response<HelpArticleModel>>();

            return NormalizeArticleResponse(response, result.IsSuccessStatusCode, result.StatusCode);
        }
        catch (Exception)
        {
            return ConnectionError<HelpArticleModel>();
        }
    }

    public async Task<Response<bool>> DeleteArticle(Guid articleId)
    {
        try
        {
            var result = await _http.DeleteAsync(
                $"api/Help/admin/articles/{articleId}");
            var response = await result.Content
                .ReadFromJsonAsync<Response<bool>>();

            return response ?? new Response<bool>
            {
                Success = false,
                Message = result.IsSuccessStatusCode
                    ? "Unexpected response from server"
                    : $"Server error: {result.StatusCode}"
            };
        }
        catch (Exception)
        {
            return ConnectionError<bool>();
        }
    }

    private static string BuildArticlesUrl(
        string path,
        string? category,
        string? status,
        string? search,
        int page,
        int pageSize)
    {
        var url = $"{path}?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(category) &&
            !category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            url += $"&category={Uri.EscapeDataString(category)}";
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            !status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            url += $"&status={Uri.EscapeDataString(HelpArticleDisplayStatus.ToApi(status))}";
        }

        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        return url;
    }

    private static object ToSavePayload(SaveHelpArticleModel request) =>
        new
        {
            title = request.Title.Trim(),
            category = request.Category.Trim(),
            content = request.Content.Trim(),
            status = HelpArticleDisplayStatus.ToApi(request.Status),
            heroImageUrl = string.IsNullOrWhiteSpace(request.HeroImageUrl)
                ? null
                : request.HeroImageUrl.Trim(),
            steps = request.Steps.Select(s => new
            {
                id = s.Id,
                sortOrder = s.SortOrder,
                detail = s.Detail,
                imageUrl = s.ImageUrl
            })
        };

    private static Response<PaginatedResponse<HelpArticleModel>> NormalizeArticlesResponse(
        Response<PaginatedResponse<HelpArticleModel>>? result)
    {
        if (result?.Data?.Data == null)
        {
            return result ?? new Response<PaginatedResponse<HelpArticleModel>>
            {
                Success = false,
                Message = "Unexpected response from server"
            };
        }

        foreach (var article in result.Data.Data)
            article.Status = HelpArticleDisplayStatus.FromApi(article.Status);

        return result;
    }

    private static Response<HelpArticleModel> NormalizeArticleResponse(
        Response<HelpArticleModel>? result,
        bool isSuccessStatusCode = true,
        System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
    {
        if (result?.Data == null)
        {
            return result ?? new Response<HelpArticleModel>
            {
                Success = false,
                Message = isSuccessStatusCode
                    ? "Unexpected response from server"
                    : $"Server error: {statusCode}"
            };
        }

        result.Data.Status = HelpArticleDisplayStatus.FromApi(result.Data.Status);
        return result;
    }

    private static Response<T> ConnectionError<T>() =>
        new()
        {
            Success = false,
            Message = "Unable to connect to server. Please try again."
        };
}
