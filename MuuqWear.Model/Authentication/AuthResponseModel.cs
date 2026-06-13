namespace MuuqWear.Model.Authentication;
public class AuthResponseModel
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? Email { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Role { get; set; }
}
