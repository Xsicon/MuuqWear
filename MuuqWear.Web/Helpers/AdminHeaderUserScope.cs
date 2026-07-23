using System.Security.Claims;

namespace MuuqWear.Web.Helpers;

/// <summary>
/// Consistent user scope for admin header read-state storage.
/// </summary>
public static class AdminHeaderUserScope
{
    public static string GetUserId(ClaimsPrincipal user) =>
        user.FindFirst("UserId")?.Value ?? string.Empty;
}
