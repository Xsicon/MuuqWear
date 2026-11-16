using MuuqWear.Application.Services.CareerPageSettingsService;
using MuuqWear.Model.CareerPage;

namespace MuuqWear.Web.Services;

public sealed class FirestoreCareerPageSettingsService : ICareerPageSettingsService
{
    private readonly AdminFirestore _store;
    private readonly object _gate = new();
    private CareerPageSettingsModel? _cache;

    public FirestoreCareerPageSettingsService(AdminFirestore store) => _store = store;

    public event Action? SettingsChanged;

    public CareerPageSettingsModel GetSettings()
    {
        lock (_gate)
        {
            _cache ??= _store.GetCareerPageSettingsAsync().GetAwaiter().GetResult();
            return _cache.Clone();
        }
    }

    public void UpdateSettings(CareerPageSettingsModel settings)
    {
        var next = new CareerPageSettingsModel
        {
            HeroEyebrow = settings.HeroEyebrow?.Trim() ?? string.Empty,
            HeroTitle = settings.HeroTitle?.Trim() ?? string.Empty,
            CultureDescription = settings.CultureDescription?.Trim() ?? string.Empty
        };
        _store.SaveCareerPageSettingsAsync(next).GetAwaiter().GetResult();
        lock (_gate)
            _cache = next;
        SettingsChanged?.Invoke();
    }
}
