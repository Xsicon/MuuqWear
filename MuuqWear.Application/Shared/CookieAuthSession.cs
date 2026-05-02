namespace MuuqWear.Application.Shared;
public class CookieAuthSession
{
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? UserId { get; set; }
    public string? ReturnUrl { get; set; }
    public bool? RememberMe { get; set; }
}
