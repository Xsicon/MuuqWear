using System.Text.Json;
using System.Text.Json.Serialization;
using MuuqWear.Model.Muuqsimo;

namespace MuuqWear.Application.Shared;

public static class EventPageContentSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static MuuqsimoPageContentModel Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return CreateDefault();

        try
        {
            var model = JsonSerializer.Deserialize<MuuqsimoPageContentModel>(json, Options);
            return EnsureInitialized(model ?? new MuuqsimoPageContentModel());
        }
        catch
        {
            return CreateDefault();
        }
    }

    public static string Serialize(MuuqsimoPageContentModel model) =>
        JsonSerializer.Serialize(EnsureInitialized(model), Options);

    public static MuuqsimoPageContentModel CreateDefault() =>
        EnsureInitialized(new MuuqsimoPageContentModel
        {
            Slug = "muuqsimo-2025",
            HeroSlides = { new MuuqsimoHeroSlideModel() },
            Schedule = { new MuuqsimoScheduleEntryModel() },
            Awards = { new MuuqsimoAwardModel() },
            Team = { new MuuqsimoTeamMemberModel() },
            Sponsors = { new MuuqsimoSponsorModel() }
        });

    public static MuuqsimoPageContentModel EnsureInitialized(MuuqsimoPageContentModel model)
    {
        model.Venue ??= new MuuqsimoVenueModel();
        model.DressCode ??= new MuuqsimoDressCodeModel();
        model.Experience ??= new MuuqsimoExperienceModel();
        model.BlueVeilWalk ??= new MuuqsimoBlueVeilWalkModel();
        model.RunwayShow ??= new MuuqsimoRunwayShowModel();
        model.Tickets ??= new MuuqsimoTicketsSectionModel();
        model.ClosingCta ??= new MuuqsimoClosingCtaModel();
        model.HeroSlides ??= new();
        model.Schedule ??= new();
        model.Awards ??= new();
        model.Team ??= new();
        model.Sponsors ??= new();
        model.Experience.Cards ??= new();
        model.BlueVeilWalk.Specs ??= new();
        model.RunwayShow.Collections ??= new();
        model.RunwayShow.FeaturedModels ??= new();
        model.RunwayShow.MusicLighting ??= new();
        model.RunwayShow.Production ??= new();
        model.Tickets.TierConfigs ??= new();
        model.Tickets.AffiliateBenefits ??= new();
        return model;
    }
}
