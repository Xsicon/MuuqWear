using MuuqWear.Model.CareerPage;

namespace MuuqWear.Application.Services.CareerPageSettingsService;

public interface ICareerPageSettingsService
{
    event Action? SettingsChanged;

    CareerPageSettingsModel GetSettings();

    void UpdateSettings(CareerPageSettingsModel settings);
}
