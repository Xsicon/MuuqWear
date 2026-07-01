namespace MuuqWear.Model.Vote;

public class VoteItemModel
{
    public Guid Id { get; set; }
    public string? StyleName { get; set; }
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Tag { get; set; }
    public int VoteCount { get; set; }
    public List<string> ColorOptions { get; set; } = new();
    public string? Status { get; set; }
    public string? Season { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool HasVoted { get; set; }
    public bool HasPreOrdered { get; set; }

    // color the current user picked when they voted (null if not voted)
    public string? VotedColor { get; set; }
}

public class VoteStatsModel
{
    public long TotalVotesThisWeek { get; set; }
    public string? MostWantedColor { get; set; }
    public string? NextDeadline { get; set; }
}

public class CastVoteModel
{
    public Guid VoteItemId { get; set; }
    public string? PreferredColor { get; set; }
}

public class PreOrderModel
{
    public Guid VoteItemId { get; set; }
}
