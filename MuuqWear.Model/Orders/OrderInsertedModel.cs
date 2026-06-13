using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Orders;

[Table("orders")]
public class OrderInsertModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("order_number")]
    public string? OrderNumber { get; set; }
}
