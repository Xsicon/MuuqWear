namespace MuuqWear.Model.Muuqsimo;

public class MuuqsimoPageModel
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public MuuqsimoPageContentModel Content { get; set; } = new();
    public List<MuuqsimoTicketTierModel> TicketTiers { get; set; } = new();
}

public class MuuqsimoPageContentModel
{
    public string? Slug { get; set; }
    public string? Tagline { get; set; }
    public string? Eyebrow { get; set; }
    public string? Subtitle { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? CountdownUtc { get; set; }
    public MuuqsimoVenueModel? Venue { get; set; }
    public MuuqsimoDressCodeModel? DressCode { get; set; }
    public List<MuuqsimoHeroSlideModel> HeroSlides { get; set; } = new();
    public MuuqsimoExperienceModel? Experience { get; set; }
    public MuuqsimoBlueVeilWalkModel? BlueVeilWalk { get; set; }
    public List<MuuqsimoScheduleEntryModel> Schedule { get; set; } = new();
    public MuuqsimoRunwayShowModel? RunwayShow { get; set; }
    public List<MuuqsimoAwardModel> Awards { get; set; } = new();
    public MuuqsimoTicketsSectionModel? Tickets { get; set; }
    public List<MuuqsimoTeamMemberModel> Team { get; set; } = new();
    public List<MuuqsimoSponsorModel> Sponsors { get; set; } = new();
    public MuuqsimoClosingCtaModel? ClosingCta { get; set; }
}

public class MuuqsimoVenueModel
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Accessibility { get; set; }
    public string? Transit { get; set; }
    public string? Parking { get; set; }
}

public class MuuqsimoDressCodeModel
{
    public string? Theme { get; set; }
    public string? Description { get; set; }
}

public class MuuqsimoHeroSlideModel
{
    public string ImageUrl { get; set; } = string.Empty;
    public string Alt { get; set; } = string.Empty;
}

public class MuuqsimoExperienceModel
{
    public string? Eyebrow { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public List<MuuqsimoExperienceCardModel> Cards { get; set; } = new();
}

public class MuuqsimoExperienceCardModel
{
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

public class MuuqsimoBlueVeilWalkModel
{
    public string? Eyebrow { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? ImageUrl { get; set; }
    public List<MuuqsimoLabelValueModel> Specs { get; set; } = new();
}

public class MuuqsimoLabelValueModel
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class MuuqsimoScheduleEntryModel
{
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class MuuqsimoRunwayShowModel
{
    public string? Eyebrow { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public List<MuuqsimoCollectionModel> Collections { get; set; } = new();
    public List<MuuqsimoFeaturedModelModel> FeaturedModels { get; set; } = new();
    public string? ModelsExtra { get; set; }
    public List<MuuqsimoMusicLightingModel> MusicLighting { get; set; } = new();
    public List<MuuqsimoProductionCreditModel> Production { get; set; } = new();
}

public class MuuqsimoCollectionModel
{
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class MuuqsimoFeaturedModelModel
{
    public string Initials { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Credit { get; set; } = string.Empty;
}

public class MuuqsimoMusicLightingModel
{
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class MuuqsimoProductionCreditModel
{
    public string Role { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}

public class MuuqsimoAwardModel
{
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Prize { get; set; } = string.Empty;
}

public class MuuqsimoTicketsSectionModel
{
    public string? CapacityNote { get; set; }
    public string? Subtitle { get; set; }
    public string? PressContact { get; set; }
    public List<MuuqsimoTicketTierConfigModel> TierConfigs { get; set; } = new();
    public List<MuuqsimoAffiliateBenefitModel> AffiliateBenefits { get; set; } = new();
}

public class MuuqsimoTicketTierConfigModel
{
    public Guid ProductId { get; set; }
    public int TotalCapacity { get; set; }
    public List<string> Perks { get; set; } = new();
    public bool IsHighlighted { get; set; }
    public string? Badge { get; set; }
}

public class MuuqsimoAffiliateBenefitModel
{
    public string Tier { get; set; } = string.Empty;
    public string Benefit { get; set; } = string.Empty;
    public string DotColor { get; set; } = string.Empty;
}

public class MuuqsimoTicketTierModel
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int Stock { get; set; }
    public string Availability { get; set; } = string.Empty;
    public List<string> Perks { get; set; } = new();
    public bool IsHighlighted { get; set; }
    public string? Badge { get; set; }
}

public class MuuqsimoTeamMemberModel
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class MuuqsimoSponsorModel
{
    public string Role { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class MuuqsimoClosingCtaModel
{
    public string? Eyebrow { get; set; }
    public string? Title { get; set; }
    public string? ImageUrl { get; set; }
}

public class MuuqsimoEventSummaryModel
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Tagline { get; set; }
    public string? StartDate { get; set; }
}
