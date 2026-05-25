namespace MuuqWear.Model.AffiliateApplication;

public class AffiliateApplicationModel
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<SocialHandleModel> SocialHandles { get; set; } = new();
    public int AudienceSize { get; set; }
    public string ContentNiche { get; set; } = string.Empty;
    public string? PortfolioUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? AdminNotes { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? WhyMuuqwear { get; set; }
    public List<string>? SampleFiles { get; set; }
    public string FormattedDate => SubmittedAt.ToString("MMM dd, yyyy");

}

