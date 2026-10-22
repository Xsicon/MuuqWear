using MuuqWear.Web.Constants;

namespace MuuqWear.Web.Services;

public static class StaffUserRules
{
    public static bool IsStaff(IReadOnlyDictionary<string, object>? claims)
    {
        if (claims is null)
            return false;
        if (IsAdminClaim(claims))
            return true;
        var role = ReadRole(claims);
        return AdminPortalRoles.IsAllowed(role);
    }

    public static bool IsAdminClaim(IReadOnlyDictionary<string, object>? claims)
    {
        if (claims is null)
            return false;
        if (claims.TryGetValue("admin", out var admin) && IsTrue(admin))
            return true;
        return string.Equals(ReadRole(claims), AdminPortalRoles.Admin, StringComparison.OrdinalIgnoreCase);
    }

    public static string ReadRole(IReadOnlyDictionary<string, object>? claims)
    {
        if (claims is null)
            return "";
        if (claims.TryGetValue("role", out var role) && role is not null)
            return role.ToString()?.Trim() ?? "";
        if (claims.TryGetValue("app_role", out var appRole) && appRole is not null)
            return appRole.ToString()?.Trim() ?? "";
        return IsTrue(claims.GetValueOrDefault("admin")) ? AdminPortalRoles.Admin : "";
    }

    public static Dictionary<string, object> ClaimsFor(string role)
    {
        var key = role.Trim();
        var isAdmin = key.Equals(AdminPortalRoles.Admin, StringComparison.OrdinalIgnoreCase);
        return new Dictionary<string, object>
        {
            ["role"] = key,
            ["admin"] = isAdmin
        };
    }

    public static string? GuardSelf(string actorUid, string targetUid) =>
        string.Equals(actorUid, targetUid, StringComparison.Ordinal)
            ? "You cannot change or disable your own account."
            : null;

    public static string? GuardLastAdmin(bool targetIsAdmin, bool disablingOrDemoting, int adminCount) =>
        targetIsAdmin && disablingOrDemoting && adminCount <= 1
            ? "Keep at least one Admin account."
            : null;

    private static bool IsTrue(object? value) =>
        value is true
        || (value is string text && (text.Equals("true", StringComparison.OrdinalIgnoreCase) || text == "1"));
}
