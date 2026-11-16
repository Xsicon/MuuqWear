using Google.Cloud.Firestore;
using MuuqWear.Application.Services.VoteService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Vote;
using MuuqWear.Web.ServerAuth;

namespace MuuqWear.Web.Services;

public sealed class FirestoreVoteService : IVoteService
{
    private readonly FirestoreDb _db;
    private readonly AdminFirestore _store;
    private readonly IHttpContextAccessor _http;

    public FirestoreVoteService(IConfiguration config, IHttpContextAccessor http, AdminFirestore store)
    {
        _db = FirestoreDb.Create(config["Firebase:ProjectId"] ?? "muuqwear-6e2e0");
        _http = http;
        _store = store;
    }

    public async Task<Response<List<VoteItemModel>>> GetActiveItems() =>
        Response<List<VoteItemModel>>.SuccessResponse(await List("active"));

    public Task<Response<List<VoteItemModel>>> GetActiveItemsPublic() => GetActiveItems();

    public async Task<Response<List<VoteItemModel>>> GetFinishedItems() =>
        Response<List<VoteItemModel>>.SuccessResponse(await List("finished"));

    public async Task<Response<VoteStatsModel>> GetStats()
    {
        var active = await List("active");
        return Response<VoteStatsModel>.SuccessResponse(new VoteStatsModel
        {
            TotalVotesThisWeek = active.Sum(v => v.VoteCount)
        });
    }

    public async Task<Response<VoteItemModel>> CastVote(Guid voteItemId, string? preferredColor = null)
    {
        var doc = _db.Collection("communityVotes").Document(voteItemId.ToString());
        await doc.UpdateAsync("voteCount", FieldValue.Increment(1));
        var (uid, _, _) = VerifiedAuthSession.Shopper(_http.HttpContext?.User);
        if (!string.IsNullOrWhiteSpace(uid))
        {
            await doc.Collection("ballots").Document(uid).SetAsync(new Dictionary<string, object>
            {
                ["color"] = preferredColor ?? "",
                ["at"] = Timestamp.FromDateTime(DateTime.UtcNow)
            });
        }
        var snap = await doc.GetSnapshotAsync();
        return Response<VoteItemModel>.SuccessResponse(AdminFirestore.ToVoteItem(snap));
    }

    public Task<Response<bool>> RegisterPreOrder(Guid voteItemId) =>
        Task.FromResult(Response<bool>.SuccessResponse(true));

    public async Task<Response<VoteItemModel>> UpsertItem(VoteItemModel item) =>
        Response<VoteItemModel>.SuccessResponse(await _store.UpsertVoteAsync(item));

    public async Task<Response<VoteItemModel>> UpdateItemStatus(Guid voteItemId, string status, string? tag = null)
    {
        var updated = await _store.UpdateVoteStatusAsync(voteItemId, status, tag);
        return updated is null
            ? Response<VoteItemModel>.Fail("Campaign not found")
            : Response<VoteItemModel>.SuccessResponse(updated);
    }

    public async Task<Response<bool>> DeleteItem(Guid voteItemId) =>
        Response<bool>.SuccessResponse(await _store.DeleteVoteAsync(voteItemId));

    private async Task<List<VoteItemModel>> List(string status)
    {
        try
        {
            var snap = await _db.Collection("communityVotes").GetSnapshotAsync();
            return snap.Documents.Select(AdminFirestore.ToVoteItem)
                .Where(v =>
                {
                    var display = (v.Status ?? "").ToLowerInvariant();
                    if (status == "active")
                        return string.IsNullOrWhiteSpace(display)
                            || display.Contains("active")
                            || display.Contains("draft")
                            || display.Contains("pre-order")
                            || display.Contains("production");
                    return display.Contains("complete")
                        || display.Contains("finished")
                        || display.Contains("closed");
                })
                .ToList();
        }
        catch (Exception)
        {
            return [];
        }
    }
}
