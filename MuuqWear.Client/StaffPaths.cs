namespace MuuqWear.Client;

/// <summary>
/// Paths that stay on InteractiveServer in MuuqWear.Web (admin, profile, staff tools).
/// </summary>
public static class StaffPaths
{
    private static readonly string[] Exact =
    [
        "/community",
        "/help",
        "/careers",
        "/affiliate",
        "/shipping-return",
        "/design-history",
        "/partner-store",
        "/not-found",
        "/error",
        "/weather"
    ];

    private static readonly string[] Prefixes =
    [
        "/admin",
        "/muuqsimo",
        "/order-confirmed"
    ];

    public static bool IsServerOwned(string? path)
    {
        var value = Normalize(path);
        foreach (var prefix in Prefixes)
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (var exact in Exact)
        {
            if (string.Equals(value, exact, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static string Normalize(string? path)
    {
        var value = string.IsNullOrWhiteSpace(path) ? "/" : path.Trim();
        var cut = value.IndexOfAny(['?', '#']);
        if (cut >= 0)
            value = value[..cut];
        if (!value.StartsWith('/'))
            value = "/" + value;
        if (value.Length > 1 && value.EndsWith('/'))
            value = value.TrimEnd('/');
        return value;
    }
}
