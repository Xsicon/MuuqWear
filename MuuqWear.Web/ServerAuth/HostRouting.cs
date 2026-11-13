using MuuqWear.Web.Constants;

namespace MuuqWear.Web.ServerAuth;

public static class HostRouting
{
    public static string? RedirectFor(string host, string pathAndQuery, string publicHost, string adminHost)
    {
        var admin = NormalizeHost(adminHost);
        if (string.IsNullOrEmpty(admin))
            return null;

        var requestHost = NormalizeHost(host);
        var path = PathOnly(pathAndQuery);

        if (requestHost.Equals(admin, StringComparison.OrdinalIgnoreCase))
        {
            if (path == "/")
                return AdminPortalPaths.Login;

            if (!AllowedOnAdmin(path))
                return "https://" + PublicName(publicHost) + WithLeadingSlash(pathAndQuery);

            return null;
        }

        if (path.Equals("/admin", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase))
            return "https://" + admin + WithLeadingSlash(pathAndQuery);

        return null;
    }

    public static string AdminSignInUrl(string? adminHost, string? publicHost)
    {
        var admin = NormalizeHost(adminHost);
        var host = string.IsNullOrEmpty(admin) ? PublicName(publicHost) : admin;
        return "https://" + host + AdminPortalPaths.Login;
    }

    public static string NormalizeHost(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var trimmed = value.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute))
            return absolute.Host.ToLowerInvariant();

        var host = trimmed.Split('/')[0];
        var colon = host.LastIndexOf(':');
        if (colon > 0 && host.IndexOf(':') == colon)
            host = host[..colon];
        return host.ToLowerInvariant();
    }

    private static string PublicName(string? publicHost)
    {
        var host = NormalizeHost(publicHost);
        return string.IsNullOrEmpty(host) ? "muuqwear.com" : host;
    }

    private static string PathOnly(string? pathAndQuery)
    {
        if (string.IsNullOrWhiteSpace(pathAndQuery))
            return "/";

        var value = pathAndQuery.Trim();
        var query = value.IndexOf('?');
        var path = query >= 0 ? value[..query] : value;
        if (string.IsNullOrEmpty(path))
            return "/";
        if (!path.StartsWith('/'))
            path = "/" + path;
        if (path.Length > 1 && path.EndsWith('/'))
            path = path.TrimEnd('/');
        return path;
    }

    private static string WithLeadingSlash(string? pathAndQuery)
    {
        if (string.IsNullOrWhiteSpace(pathAndQuery))
            return "/";
        return pathAndQuery.StartsWith('/') ? pathAndQuery : "/" + pathAndQuery;
    }

    private static bool AllowedOnAdmin(string path)
    {
        if (path.Equals("/admin", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.Equals("/_blazor", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/_blazor/", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.Equals("/_framework", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/_framework/", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.Equals("/_content", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/_content/", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.Equals("/auth/staff-login", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.Equals("/auth/clear", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.Equals("/logout", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.StartsWith("/bootstrap/", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.StartsWith("/apple-touch-icon", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.Equals("/MuuqWear.Web.styles.css", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.EndsWith(".bundle.scp.css", StringComparison.OrdinalIgnoreCase)
            && path.IndexOf('/', 1) < 0)
            return true;
        if (path.Equals("/app.css", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.Equals("/not-found", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.Equals("/error", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.Equals("/admin/access-denied", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }
}
