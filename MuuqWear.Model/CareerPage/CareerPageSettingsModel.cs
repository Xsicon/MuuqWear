namespace MuuqWear.Model.CareerPage;

public class CareerPageSettingsModel
{
    public string HeroEyebrow { get; set; } = "Join the Atelier";
    public string HeroTitle { get; set; } = "Craft the Future.";
    public string CultureDescription { get; set; } =
        "Join the team shaping minimalist luxury. We're looking for people who believe that the constraint is the freedom.";

    public CareerPageSettingsModel Clone() => new()
    {
        HeroEyebrow = HeroEyebrow,
        HeroTitle = HeroTitle,
        CultureDescription = CultureDescription
    };
}
