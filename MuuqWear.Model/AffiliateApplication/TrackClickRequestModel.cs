namespace MuuqWear.Model.AffiliateApplication;

public class TrackClickRequestModel
{
    public string AffiliateCode { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? ReferrerUrl { get; set; }
}
