namespace MuuqWear.Model.Chat;

public class ChatSessionModel
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? Email { get; set; }
    public string? GuestEmail { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastActivity { get; set; }
    public string? LastMessagePreview { get; set; }
    public string? LastMessageSender { get; set; }
    public DateTime CreatedAt { get; set; }

    public string? ContactEmail =>
        !string.IsNullOrWhiteSpace(CustomerEmail) ? CustomerEmail :
        !string.IsNullOrWhiteSpace(Email) ? Email :
        !string.IsNullOrWhiteSpace(GuestEmail) ? GuestEmail : null;
}
