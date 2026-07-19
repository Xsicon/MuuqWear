namespace MuuqWear.Web.Components.Pages.AdminComponent;

using MuuqWear.Application.Shared;

public static class AffiliateAdminDesignHelper
{
    public record TierTheme(
        string Name,
        string Color,
        string BgColor,
        string TextColor,
        string DotColor);

    public static readonly TierTheme[] TierThemes =
    [
        new("Bronze", "#CD7F32", "#FEF3E8", "#7A3F00", "#CD7F32"),
        new("Silver", "#8A8A8A", "#F3F4F6", "#374151", "#9CA3AF"),
        new("Gold", "#F59E0B", "#FEF3C7", "#92400E", "#F59E0B")
    ];

    public static TierTheme GetTierTheme(string? slugOrName)
    {
        if (string.IsNullOrWhiteSpace(slugOrName))
            return TierThemes[0];

        var key = AffiliateTierCatalog.NormalizeTierSlug(slugOrName);
        return TierThemes.FirstOrDefault(t =>
                   t.Name.Equals(key, StringComparison.OrdinalIgnoreCase) ||
                   t.Name.Equals(FormatTierLabel(key), StringComparison.OrdinalIgnoreCase))
               ?? TierThemes[0];
    }

    public static string FormatTierLabel(string tier)
    {
        var normalized = AffiliateTierCatalog.NormalizeTierSlug(tier);
        if (normalized.Equals("none", StringComparison.OrdinalIgnoreCase))
            return "Bronze";

        return char.ToUpperInvariant(normalized[0]) + normalized[1..].ToLowerInvariant();
    }

    public static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "?";

        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();

        return char.ToUpperInvariant(name[0]).ToString();
    }

    public static string FormatFollowers(int audienceSize) =>
        audienceSize >= 1000
            ? audienceSize.ToString("N0")
            : audienceSize.ToString();

    public static string GetPrimaryHandle(IReadOnlyList<MuuqWear.Model.AffiliateApplication.SocialHandleModel>? handles)
    {
        var first = handles?.FirstOrDefault(h => !string.IsNullOrWhiteSpace(h.Handle));
        if (first == null || string.IsNullOrWhiteSpace(first.Handle))
            return string.Empty;

        var handle = first.Handle.Trim();
        return handle.StartsWith('@') ? handle : $"@{handle}";
    }

    public static IReadOnlyList<string> GetDefaultPerks(string slug) =>
        slug.ToLowerInvariant() switch
        {
            "silver" =>
            [
                "All Bronze perks",
                "Priority support",
                "Early access to drops",
                "Quarterly bonus"
            ],
            "gold" =>
            [
                "All Silver perks",
                "Dedicated account manager",
                "Exclusive campaign invites",
                "Co-branded content"
            ],
            _ =>
            [
                "Custom referral link",
                "Monthly newsletter",
                "Muuqwear branded kit"
            ]
        };

    public static (string Bg, string Fg) GetStatusColors(string status) =>
        status.ToLowerInvariant() switch
        {
            "approved" => ("#D1FAE5", "#065F46"),
            "rejected" or "denied" => ("#FEE2E2", "#991B1B"),
            "waitlisted" => ("#EDE9FE", "#5B21B6"),
            _ => ("#FEF3C7", "#92400E")
        };
}
