using MuuqWear.Application.Shared;
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
                publicTiers = AffiliateTierCatalog.FilterActiveCanonical(result.Data);
            }

            if (publicTiers.Count == 0)
            {
                Console.WriteLine(
                    $"[Affiliate] LoadPublicTiers fallback: Success={result?.Success}, Message={result?.Message}");
                publicTiers = AffiliateTierCatalog.DefaultTiers.ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Affiliate] LoadPublicTiers error: {ex.Message}");
            publicTiers = AffiliateTierCatalog.DefaultTiers.ToList();
        }
        finally
        {
            tiersLoaded = true;
            UpdateTierFaqAnswer();
        }
    }

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

        if (tiers.Count <= 1)
            return false;

        var middleIndex = (tiers.Count - 1) / 2;
        return tier.SortOrder == tiers.OrderBy(t => t.SortOrder).ElementAt(middleIndex).SortOrder;
    }

    private static IEnumerable<string> GetFallbackTierPerks(string slug) =>
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

    private static IEnumerable<string> FormatTierPerks(AffiliateTierModel tier)
    {
        var perks = tier.Perks?.Count > 0 ? tier.Perks : GetFallbackTierPerks(tier.Slug);
        return perks.Select(p => p.Replace("{commission}", tier.CommissionPerTenLabel, StringComparison.Ordinal));
    }
}
