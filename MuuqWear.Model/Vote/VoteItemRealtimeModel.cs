using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Vote;

[Table("vote_items")]
public class VoteItemRealtimeModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("vote_count")]
    public int VoteCount { get; set; }
}
