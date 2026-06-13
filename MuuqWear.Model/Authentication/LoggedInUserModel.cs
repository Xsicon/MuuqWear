using System.Security.Claims;

namespace MuuqWear.Model.Authentication;
public class LoggedInUserModel
{
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public string UserId { get; set; } = "";

    public static LoggedInUserModel FromClaimsPrincipal(ClaimsPrincipal principal) => new()
    {
        UserName = principal.FindFirst(ClaimTypes.Name)?.Value ?? "",
        Email = principal.FindFirst(ClaimTypes.Email)?.Value ?? "",
        Role = principal.FindFirst(ClaimTypes.Role)?.Value ?? "",
        AccessToken = principal.FindFirst("AccessToken")?.Value ?? "",
        UserId = principal.FindFirst("UserId")?.Value ?? ""
    };
}

