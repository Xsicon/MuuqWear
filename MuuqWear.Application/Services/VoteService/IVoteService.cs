using MuuqWear.Application.Shared;
using MuuqWear.Model.Vote;

namespace MuuqWear.Application.Services.VoteService;
public interface IVoteService
{
    Task<Response<List<VoteItemModel>>> GetActiveItems();
    Task<Response<List<VoteItemModel>>> GetActiveItemsPublic(); // ← add

    Task<Response<List<VoteItemModel>>> GetFinishedItems();
    Task<Response<VoteStatsModel>> GetStats();
    Task<Response<VoteItemModel>> CastVote(Guid voteItemId, string? preferredColor = null);
    Task<Response<bool>> RegisterPreOrder(Guid voteItemId);
}
