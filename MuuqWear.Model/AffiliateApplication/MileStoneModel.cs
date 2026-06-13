namespace MuuqWear.Model.AffiliateApplication;

public class MilestoneModel
{
    public int ItemsRequired { get; set; }
    public decimal BonusAmount { get; set; }
    public bool IsAchieved { get; set; }
    public int ItemsToGo { get; set; }

    public string Title => $"{ItemsRequired} Items Sold";
    public string StatusText => IsAchieved
        ? "Milestone achieved!"
        : $"{ItemsToGo} items to go";
}
