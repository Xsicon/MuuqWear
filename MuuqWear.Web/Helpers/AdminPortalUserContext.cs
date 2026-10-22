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
                   ?? user.FindFirst(ClaimTypes.Email)?.Value
                   ?? user.FindFirst("email")?.Value
                   ?? "Admin";

        var email = user.FindFirst(ClaimTypes.Email)?.Value
                    ?? user.FindFirst("email")?.Value;
        var trimmedName = name.Trim();
        if (string.Equals(trimmedName, "Admin", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(email))
            trimmedName = email.Trim();
        if (trimmedName.Contains('@'))
            trimmedName = trimmedName.Split('@')[0];
        var parts = trimmedName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var initials = parts.Length >= 2
            ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
            : parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();

        var role = GetRole(user);
        var roleDisplay = AdminPortalRoles.GetDisplayName(role);

        return (trimmedName, initials, roleDisplay);
    }

    public static string? GetRole(ClaimsPrincipal user)
    {
        if (user.FindFirst("admin") is { } adminClaim
            && (string.Equals(adminClaim.Value, "true", StringComparison.OrdinalIgnoreCase)
                || adminClaim.Value == "1"))
            return AdminPortalRoles.Admin;

        if (user.IsInRole(AdminPortalRoles.Admin))
            return AdminPortalRoles.Admin;

        var role = user.FindFirst(ClaimTypes.Role)?.Value
            ?? user.FindFirst("role")?.Value
            ?? user.FindFirst("app_role")?.Value
            ?? user.Claims.FirstOrDefault(claim =>
                claim.Type.EndsWith("/role", StringComparison.OrdinalIgnoreCase))?.Value;
        return string.IsNullOrWhiteSpace(role) ? null : role.Trim();
    }
}
