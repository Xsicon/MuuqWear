public class MagicLinkVerifyModel
{
    // JWT access token from URL fragment
    public string? AccessToken { get; set; }

    // refresh token from URL fragment
    public string? RefreshToken { get; set; }
}