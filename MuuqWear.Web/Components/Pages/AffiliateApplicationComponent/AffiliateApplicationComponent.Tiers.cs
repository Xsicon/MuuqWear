using MuuqWear.Model.AffiliateApplication;

namespace MuuqWear.Web.Components.Pages.AffiliateApplicationComponent;

public partial class AffiliateApplicationComponent
{
    private List<AffiliateTierModel> publicTiers = new();
    private bool tiersLoaded;

    private async Task LoadPublicTiersAsync()
    {
        try
        {
            var result = await AffiliateService.GetTiers();
            if (result?.Data is { Count: > 0 })
            {
                publicTiers = result.Data
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.SortOrder)
                    .ToList();
            }
            else
            {
                Console.WriteLine(
                    $"[Affiliate] LoadPublicTiers fallback: Success={result?.Success}, Message={result?.Message}");
                publicTiers = GetDefaultTiers();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Affiliate] LoadPublicTiers error: {ex.Message}");
            publicTiers = GetDefaultTiers();
        }
        finally
        {
            tiersLoaded = true;
            UpdateTierFaqAnswer();
        }
    }

    private static List<AffiliateTierModel> GetDefaultTiers() =>
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

    private void UpdateTierFaqAnswer()
    {
        if (publicTiers.Count == 0)
            return;

        var tierParts = publicTiers.Select(t =>
            t.ItemsSoldThreshold <= 0
                ? $"{t.DisplayName} is your starting tier"
                : $"{t.DisplayName} unlocks at {t.ItemsSoldThreshold} items");

        var faq = faqs.FirstOrDefault(f =>
            f.Question.Contains("level up", StringComparison.OrdinalIgnoreCase));

        if (faq != null)
        {
            faq.Answer =
                $"Every item sold through your link counts as 1 XP. {string.Join(", ", tierParts)}. " +
                "Track your progress in real-time on your dashboard.";
        }
    }

    private static string GetTierCssClass(string slug) =>
        slug.ToLowerInvariant() switch
        {
            "silver" => "mw-tier-silver",
            "gold" => "mw-tier-gold",
            _ => "mw-tier-bronze"
        };

    private static bool IsFeaturedTier(AffiliateTierModel tier, IReadOnlyList<AffiliateTierModel> tiers)
    {
        if (tier.Slug.Equals("silver", StringComparison.OrdinalIgnoreCase))
            return true;

        return tiers.Count == 3 && tier.SortOrder == 2;
    }

    private static IEnumerable<string> GetTierPerks(string slug) =>
        slug.ToLowerInvariant() switch
        {
            "silver" =>
            [
                "25% off all personal purchases",
                "{commission} commission per 10 items",
                "Free Muuqsimo event tickets",
                "Priority support & early access"
            ],
            "gold" =>
            [
                "25% off all personal purchases",
                "{commission} commission per 10 items",
                "All-expenses-paid Muuqsimo trip",
                "VIP access & exclusive experiences"
            ],
            _ =>
            [
                "25% off all personal purchases",
                "{commission} commission per 10 items",
                "Event ticket discount",
                "Access to Ambassador Dashboard"
            ]
        };

    private static IEnumerable<string> FormatTierPerks(AffiliateTierModel tier) =>
        GetTierPerks(tier.Slug)
            .Select(p => p.Replace("{commission}", tier.CommissionPerTenLabel, StringComparison.Ordinal));
}
