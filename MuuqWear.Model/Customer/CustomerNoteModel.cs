namespace MuuqWear.Model.Customer;

public class CustomerNoteModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorRole { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
