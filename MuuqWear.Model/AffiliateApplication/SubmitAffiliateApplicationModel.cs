namespace MuuqWear.Model.AffiliateApplication;

public class SubmitAffiliateApplicationModel
{
    public List<SocialHandleModel> SocialHandles { get; set; } = new();
    public int AudienceSize { get; set; }
    public string ContentNiche { get; set; } = string.Empty;
    public string? PortfolioUrl { get; set; }
    public string? FullName { get; set; } = string.Empty;
    public string? Email { get; set; } = string.Empty;
    public string? WhyMuuqwear { get; set; } = string.Empty;
    public List<string>? SampleFiles { get; set; }


}