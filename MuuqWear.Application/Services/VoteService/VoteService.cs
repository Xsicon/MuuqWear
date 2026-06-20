using MuuqWear.Application.Shared;
using MuuqWear.Model.Vote;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.VoteService;

public class VoteService : IVoteService
{
    private readonly HttpClient _http;

    public VoteService(HttpClient http)
    {
        _http = http;
    }

    // =============================================
    // GET ACTIVE ITEMS
    // =============================================
    public async Task<Response<List<VoteItemModel>>> GetActiveItems()
    {
        try
        {
            var response = await _http
                .GetFromJsonAsync<Response<List<VoteItemModel>>>(
                    "api/Vote/active");

            return response ?? new Response<List<VoteItemModel>>
            {
                Success = false,
                Message = "Unexpected response from server"
            };
        }
        catch (Exception)
        {
            return new Response<List<VoteItemModel>>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }
    // =============================================
    // GET FINISHED ITEMS
    // =============================================
    public async Task<Response<List<VoteItemModel>>> GetFinishedItems()
    {
        try
        {
            var response = await _http
                .GetFromJsonAsync<Response<List<VoteItemModel>>>(
                    "api/Vote/finished");

            return response ?? new Response<List<VoteItemModel>>
            {
                Success = false,
                Message = "Unexpected response from server"
            };
        }
        catch (Exception)
        {
            return new Response<List<VoteItemModel>>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    // =============================================
    // GET STATS
    // =============================================
    public async Task<Response<VoteStatsModel>> GetStats()
    {
        try
        {
            var response = await _http
                .GetFromJsonAsync<Response<VoteStatsModel>>(
                    "api/Vote/stats");

            return response ?? new Response<VoteStatsModel>
            {
                Success = false,
                Message = "Unexpected response from server"
            };
        }
        catch (Exception)
        {
            return new Response<VoteStatsModel>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    // =============================================
    // CAST VOTE
    // =============================================
    public async Task<Response<VoteItemModel>> CastVote(
        Guid voteItemId,
        string? preferredColor = null)
    {
        try
        {
            var result = await _http.PostAsJsonAsync(
                "api/Vote/cast",
                new CastVoteModel
                {
                    VoteItemId = voteItemId,
                    PreferredColor = preferredColor
                });

            var response = await result.Content
                .ReadFromJsonAsync<Response<VoteItemModel>>();

            return response ?? new Response<VoteItemModel>
            {
                Success = false,
                Message = result.IsSuccessStatusCode
                    ? "Unexpected response from server"
                    : $"Server error: {result.StatusCode}"
            };
        }
        catch (Exception)
        {
            return new Response<VoteItemModel>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    // =============================================
    // REGISTER PRE-ORDER
    // =============================================
    public async Task<Response<bool>> RegisterPreOrder(Guid voteItemId)
    {
        try
        {
            var result = await _http.PostAsJsonAsync(
                "api/Vote/pre-order",
                new PreOrderModel { VoteItemId = voteItemId });

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
            return new Response<bool>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }

    public async Task<Response<List<VoteItemModel>>> GetActiveItemsPublic()
    {
        try
        {
            var response = await _http
                .GetFromJsonAsync<Response<List<VoteItemModel>>>(
                    "api/Vote/active/public");

            return response ?? new Response<List<VoteItemModel>>
            {
                Success = false,
                Message = "Unexpected response from server"
            };
        }
        catch (Exception)
        {
            return new Response<List<VoteItemModel>>
            {
                Success = false,
                Message = "Unable to connect to server. Please try again."
            };
        }
    }
}
