using MuuqWear.Model.CareerPage;

namespace MuuqWear.Application.Services.CareerPageSettingsService;

public class CareerPageSettingsService : ICareerPageSettingsService
{
    private CareerPageSettingsModel _settings = new();

    public event Action? SettingsChanged;

    public CareerPageSettingsModel GetSettings() => _settings.Clone();

    public void UpdateSettings(CareerPageSettingsModel settings)
    {
        _settings = new CareerPageSettingsModel
        {
            HeroEyebrow = settings.HeroEyebrow?.Trim() ?? string.Empty,
            HeroTitle = settings.HeroTitle?.Trim() ?? string.Empty,
            CultureDescription = settings.CultureDescription?.Trim() ?? string.Empty
        };

        SettingsChanged?.Invoke();
    }
}
