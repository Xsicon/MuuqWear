namespace MuuqWear.Model.DTO.AdminSystem;

public class SystemLogEntryModel
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }

    public string TimestampDisplay => Timestamp.ToString("MMM d, yyyy HH:mm");
}
