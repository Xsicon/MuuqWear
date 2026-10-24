using System.Security.Claims;
using MuuqWear.Application.Shared;
using MuuqWear.Web.Constants;

namespace MuuqWear.Web.ServerAuth;

public static class VerifiedAuthSession
{
    public static CookieAuthSession Create(
        string token,
        string userId,
        string? email,
        string? name,
        string role,
        string? returnUrl,
        bool rememberMe,
        string? refreshToken = null)
    {
        var safeRole = string.IsNullOrWhiteSpace(role) ? "user" : role.Trim();
        return new CookieAuthSession
        {
            Token = token,
            RefreshToken = string.IsNullOrWhiteSpace(refreshToken) ? null : refreshToken,
            UserId = userId,
            Email = email ?? "",
            Username = string.IsNullOrWhiteSpace(name) ? email ?? "" : name,
            Role = safeRole,
            ReturnUrl = SafeReturnUrl(returnUrl, safeRole),
            RememberMe = rememberMe
        };
    }

    public static string RoleFromClaims(IReadOnlyDictionary<string, object> claims)
    {
        if (IsTruthy(claims, "admin"))
            return AdminPortalRoles.Admin;

        if (TryString(claims, "role", out var role)
            || TryString(claims, "app_role", out role))
        {
            if (AdminPortalRoles.IsAllowed(role))
                return role.Trim();
        }

        return "user";
    }

    public static string SafeReturnUrl(string? returnUrl, string role)
    {
        var staff = AdminPortalRoles.IsAllowed(role);
        var fallback = staff ? AdminPortalRoles.GetDefaultHome(role) : "/profile";
        if (string.IsNullOrWhiteSpace(returnUrl))
            return fallback;
        if (!returnUrl.StartsWith('/') || returnUrl.StartsWith("//") || returnUrl.Contains('\\'))
            return fallback;
        if (staff)
            return returnUrl.StartsWith("/admin", StringComparison.Ordinal) ? returnUrl : fallback;
        if (returnUrl.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
            return fallback;
        return returnUrl;
    }

    public static (string? Uid, string? Email, string? Name) Shopper(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return default;

        var uid = user.FindFirst("UserId")?.Value;
        if (string.IsNullOrWhiteSpace(uid))
            return default;

        var email = user.FindFirst(ClaimTypes.Email)?.Value
                    ?? user.FindFirst("email")?.Value;
        var name = user.FindFirst(ClaimTypes.Name)?.Value;
        return (uid, email, name);
    }

    private static bool IsTruthy(IReadOnlyDictionary<string, object> claims, string key)
    {
        if (!claims.TryGetValue(key, out var value) || value is null)
            return false;
        if (value is bool flag)
            return flag;
        var text = value.ToString();
        return string.Equals(text, "true", StringComparison.OrdinalIgnoreCase) || text == "1";
    }

    private static bool TryString(IReadOnlyDictionary<string, object> claims, string key, out string value)
    {
        value = "";
        if (!claims.TryGetValue(key, out var raw) || raw is null)
            return false;
        var text = raw.ToString();
        if (string.IsNullOrWhiteSpace(text))
            return false;
        value = text;
        return true;
    }
}
