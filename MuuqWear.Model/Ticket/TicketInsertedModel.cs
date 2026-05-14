using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Ticket;

[Table("support_tickets")]
public class TicketInsertModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("subject")]
    public string? Subject { get; set; }
}
