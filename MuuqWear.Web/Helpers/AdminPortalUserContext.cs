using System.Security.Claims;
using MuuqWear.Web.Constants;

namespace MuuqWear.Web.Helpers;

public static class AdminPortalUserContext
{
    public static (string Name, string Initials, string RoleDisplay) FromClaims(ClaimsPrincipal user)
    {
        var name = user.FindFirst("FullName")?.Value
                   ?? user.FindFirst(ClaimTypes.Name)?.Value
                   ?? user.FindFirst("name")?.Value
                   ?? "Admin";

        var trimmedName = name.Trim();
        var parts = trimmedName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var initials = parts.Length >= 2
            ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
            : parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();

        var role = user.FindFirst(ClaimTypes.Role)?.Value;
        var roleDisplay = AdminPortalRoles.GetDisplayName(role);

        return (trimmedName, initials, roleDisplay);
    }

    public static string? GetRole(ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Role)?.Value;
}
