namespace MuuqWear.Model.JobApplication;

public class SubmitJobApplicationModel
{
    public Guid JobId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PortfolioUrl { get; set; }
    public string ResumeUrl { get; set; } = string.Empty;
}
