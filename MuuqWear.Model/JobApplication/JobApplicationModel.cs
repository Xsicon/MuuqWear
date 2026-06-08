namespace MuuqWear.Model.JobApplication;

public class JobApplicationModel
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PortfolioUrl { get; set; }
    public string ResumeUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "new";
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
