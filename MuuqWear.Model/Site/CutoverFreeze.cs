namespace MuuqWear.Model.Site;

public static class CutoverFreeze
{
    public static bool Enabled(string? flag) =>
        string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);

    public static bool Allows(string method, string? path)
    {
        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase)
            || method.Equals("HEAD", StringComparison.OrdinalIgnoreCase)
            || method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            return true;

        var route = (path ?? "").TrimEnd('/').ToLowerInvariant();
        return route is "/api/auth/login"
            or "/api/auth/logout"
            or "/api/auth/refresh-token"
            or "/api/auth/verify-magic-link"
            or "/auth/sign-in"
            or "/auth/prepare-cookie";
    }
}
