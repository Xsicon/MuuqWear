using MuuqWear.Model.AffiliateApplication;

namespace MuuqWear.Web.Components.Pages
{
    public partial class MileStone
    {
        private List<MilestoneModel>? mileStones;
        private AffiliateInfoModel? affiliateInfo;
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
                // Load affiliate info
                var result = await AffiliateService.GetAffiliateInfo();

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
    }
}
