using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.OrderReturn;

[Table("order_returns")]
public class OrderReturnInsertedModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("return_number")]
    public string? ReturnNumber { get; set; }
}
