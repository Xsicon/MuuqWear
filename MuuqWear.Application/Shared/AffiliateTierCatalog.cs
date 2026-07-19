using MuuqWear.Model.AffiliateApplication;

namespace MuuqWear.Application.Shared;

/// <summary>
/// Canonical affiliate tiers for MuuqWear (Bronze, Silver, Gold).
/// Legacy tiers such as "platinum" are excluded from marketing and admin settings UI.
/// </summary>
public static class AffiliateTierCatalog
{
    public static readonly string[] CanonicalSlugs = ["bronze", "silver", "gold"];

    public static bool IsCanonical(string? slug) =>
        !string.IsNullOrWhiteSpace(slug) &&
        CanonicalSlugs.Contains(slug, StringComparer.OrdinalIgnoreCase);

    /// <summary>Maps legacy tier slugs to a canonical tier for display.</summary>
    public static string NormalizeTierSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug) || slug.Equals("none", StringComparison.OrdinalIgnoreCase))
            return "bronze";

        if (slug.Equals("platinum", StringComparison.OrdinalIgnoreCase))
            return "gold";

        return slug.ToLowerInvariant();
    }

    public static List<AffiliateTierModel> FilterCanonical(IEnumerable<AffiliateTierModel> tiers) =>
        tiers
            .Where(t => IsCanonical(t.Slug))
            .OrderBy(t => t.SortOrder)
            .ToList();

    public static List<AffiliateTierModel> FilterActiveCanonical(IEnumerable<AffiliateTierModel> tiers) =>
        FilterCanonical(tiers.Where(t => t.IsActive));

    public static Dictionary<string, int> AggregateCanonicalCounts(
        IEnumerable<KeyValuePair<string, int>> countsByTier)
    {
        var merged = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (tier, count) in countsByTier)
        {
            var slug = NormalizeTierSlug(tier);
            if (!IsCanonical(slug))
                continue;

            merged[slug] = merged.GetValueOrDefault(slug) + count;
        }

        return merged;
    }

    public static IReadOnlyList<AffiliateTierModel> DefaultTiers { get; } =
    [
        new AffiliateTierModel
        {
            Slug = "bronze",
            DisplayName = "Bronze",
            ItemsSoldThreshold = 0,
            CommissionRatePercent = 5,
            ReferralDiscountPercent = 5,
            SortOrder = 1,
            IsActive = true
        },
        new AffiliateTierModel
        {
            Slug = "silver",
            DisplayName = "Silver",
            ItemsSoldThreshold = 150,
            CommissionRatePercent = 10,
            ReferralDiscountPercent = 10,
            SortOrder = 2,
            IsActive = true
        },
        new AffiliateTierModel
        {
            Slug = "gold",
            DisplayName = "Gold",
            ItemsSoldThreshold = 500,
            CommissionRatePercent = 15,
            ReferralDiscountPercent = 15,
            SortOrder = 3,
            IsActive = true
        }
    ];
}
