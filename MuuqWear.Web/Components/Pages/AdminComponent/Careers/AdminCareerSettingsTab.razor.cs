using Microsoft.AspNetCore.Components;
using MuuqWear.Application.Services.CareerPageSettingsService;
using MuuqWear.Model.CareerPage;

namespace MuuqWear.Web.Components.Pages.AdminComponent.Careers;

public partial class AdminCareerSettingsTab
{
    [Inject] private ICareerPageSettingsService CareerPageSettings { get; set; } = default!;

    private string heroEyebrow = string.Empty;
    private string heroTitle = string.Empty;
    private string cultureDescription = string.Empty;
    private string? toast;
    private string? validationError;

    protected override void OnInitialized()
    {
        var settings = CareerPageSettings.GetSettings();
        heroEyebrow = settings.HeroEyebrow;
        heroTitle = settings.HeroTitle;
        cultureDescription = settings.CultureDescription;
    }

    private void SaveSettings()
    {
        validationError = null;
        toast = null;

        if (string.IsNullOrWhiteSpace(heroEyebrow))
        {
            validationError = "Hero eyebrow is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(heroTitle))
        {
            validationError = "Hero title is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(cultureDescription))
        {
            validationError = "Culture description is required.";
            return;
        }

        CareerPageSettings.UpdateSettings(new CareerPageSettingsModel
        {
            HeroEyebrow = heroEyebrow,
            HeroTitle = heroTitle,
            CultureDescription = cultureDescription
        });

        toast = "Settings saved. Changes are live on the public careers page.";
    }
}
