namespace MuuqWear.Application.Shared;

public static class AdminDirectoryFilter
{
    private static readonly string[] StaffEmails =
    [
        "ops@muuqwear.com",
        "tech@muuqwear.com",
        "merch@muuqwear.com",
        "creative@muuqwear.com",
        "support@muuqwear.com"
    ];

    public static bool IsShopperEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var value = email.Trim();
        if (value.Contains("sandbox.", StringComparison.OrdinalIgnoreCase))
            return false;
        if (value.EndsWith("@demo.muuqwear.test", StringComparison.OrdinalIgnoreCase))
            return false;
        return !StaffEmails.Contains(value, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsNoiseTicket(string? subject, string? name)
    {
        var text = $"{subject} {name}";
        return text.Contains("asda", StringComparison.OrdinalIgnoreCase)
            || text.Contains("mnbjca", StringComparison.OrdinalIgnoreCase)
            || string.Equals(subject?.Trim(), "Test 2", StringComparison.OrdinalIgnoreCase)
            || string.Equals(subject?.Trim(), "Test Question", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsNoiseArticle(string? title) =>
        string.Equals(title?.Trim(), "Test Question", StringComparison.OrdinalIgnoreCase);
}
