using System.Security.Claims;
using Google.Cloud.Firestore;
using MuuqWear.Application.Services.VoteService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Vote;

namespace MuuqWear.Web.Services;

public sealed class FirestoreVoteService : IVoteService
{
    private readonly FirestoreDb _db;
    private readonly IHttpContextAccessor _http;

    public FirestoreVoteService(IConfiguration config, IHttpContextAccessor http)
    {
        _db = FirestoreDb.Create(config["Firebase:ProjectId"] ?? "muuqwear-6e2e0");
        _http = http;
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
        var uid = _http.HttpContext?.User.FindFirst("UserId")?.Value;
        if (!string.IsNullOrWhiteSpace(uid))
        {
            await doc.Collection("ballots").Document(uid).SetAsync(new Dictionary<string, object>
            {
                ["color"] = preferredColor ?? "",
                ["at"] = Timestamp.FromDateTime(DateTime.UtcNow)
            });
        }
        var snap = await doc.GetSnapshotAsync();
        return Response<VoteItemModel>.SuccessResponse(ToVote(snap));
    }

    public Task<Response<bool>> RegisterPreOrder(Guid voteItemId) =>
        Task.FromResult(Response<bool>.SuccessResponse(true));

    private async Task<List<VoteItemModel>> List(string status)
    {
        var snap = await _db.Collection("communityVotes").GetSnapshotAsync();
        return snap.Documents.Select(ToVote)
            .Where(v => string.Equals(v.Status, status, StringComparison.OrdinalIgnoreCase)
                        || (status == "active" && string.IsNullOrWhiteSpace(v.Status)))
            .ToList();
    }

    private static VoteItemModel ToVote(DocumentSnapshot doc)
    {
        Guid.TryParse(doc.Id, out var id);
        return new VoteItemModel
        {
            Id = id,
            StyleName = Str(doc, "styleName"),
            Subtitle = Str(doc, "subtitle"),
            Description = Str(doc, "description"),
            ImageUrl = Str(doc, "imageUrl"),
            Tag = Str(doc, "tag"),
            VoteCount = doc.ContainsField("voteCount") ? Convert.ToInt32(doc.GetValue<object>("voteCount")) : 0,
            Status = Str(doc, "status"),
            Season = Str(doc, "season")
        };
    }

    private static string Str(DocumentSnapshot doc, string field) =>
        doc.ContainsField(field) ? doc.GetValue<string>(field) ?? "" : "";
}
