namespace MuuqWear.Model.DTO.AdminSystem;

public class BackgroundJobModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Schedule { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public string? LastRunMessage { get; set; }

    public string LastRunDisplay =>
        LastRunAt.HasValue ? LastRunAt.Value.ToString("MMM d, yyyy h:mm tt") : "Never";

    public string NextRunDisplay =>
        NextRunAt.HasValue ? NextRunAt.Value.ToString("MMM d, yyyy h:mm tt") : "—";
}
