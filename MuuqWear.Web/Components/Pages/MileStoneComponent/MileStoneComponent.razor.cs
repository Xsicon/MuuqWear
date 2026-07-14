using MuuqWear.Model.AffiliateApplication;

namespace MuuqWear.Web.Components.Pages.MileStoneComponent
{
    public partial class MileStoneComponent
    {
        private List<MilestoneModel>? mileStones;
        private AffiliateInfoModel? affiliateInfo;
        private List<AffiliateTierModel> publicTiers = new();
        private bool isLoading = true;


        protected override async Task OnInitializedAsync()
        {
            await LoadMilestones();
        }

        private async Task LoadMilestones()
        {
            isLoading = true;
            StateHasChanged();

            try
            {
                var tiersTask = AffiliateService.GetTiers();
                var result = await AffiliateService.GetAffiliateInfo();
                var tiersResult = await tiersTask;

                if (tiersResult.Success && tiersResult.Data is { Count: > 0 })
                {
                    publicTiers = tiersResult.Data
                        .Where(t => t.IsActive)
                        .OrderBy(t => t.SortOrder)
                        .ToList();
                }
                else
                {
                    publicTiers = GetDefaultTiers();
                }

                if (result.Success && result.Data != null)
                {
                    affiliateInfo = result.Data;

                    // Calculate milestones
                    int currentXP = affiliateInfo.ItemsSold;
                    mileStones = CalculateMilestones(currentXP);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" [Milestones] Load error: {ex.Message}");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }
        private List<MilestoneModel> CalculateMilestones(int currentXP)
        {
            // Define all 5 milestones with their bonuses
            var milestoneDefinitions = new[]
            {
        new { Items = 50, Bonus = 500m },
        new { Items = 100, Bonus = 1000m },
        new { Items = 250, Bonus = 2000m },
        new { Items = 500, Bonus = 5000m },
        new { Items = 1000, Bonus = 10000m }
    };

            // Calculate progress for each milestone
            return milestoneDefinitions.Select(m => new MilestoneModel
            {
                ItemsRequired = m.Items,
                BonusAmount = m.Bonus,
                IsAchieved = currentXP >= m.Items,
                ItemsToGo = Math.Max(0, m.Items - currentXP)
            }).ToList();
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
            },
            new AffiliateTierModel
            {
                Slug = "platinum",
                DisplayName = "Platinum",
                ItemsSoldThreshold = 1000,
                CommissionRatePercent = 20,
                ReferralDiscountPercent = 20,
                SortOrder = 4,
                IsActive = true
            }
        ];

        private static string GetTierStrokeColor(string slug) =>
            slug.ToLowerInvariant() switch
            {
                "silver" => "rgb(169, 183, 204)",
                "gold" => "rgb(212, 168, 67)",
                "platinum" => "rgb(99, 102, 241)",
                _ => "rgb(205, 127, 50)"
            };
    }
}
