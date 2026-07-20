using MuuqWear.Application.Services.AdminBadgeService;
using MuuqWear.Model.AdminBadgeCount;

namespace MuuqWear.Web.Services;

/// <summary>
/// Short-lived cache for admin badge counts to reduce API chatter during navigation.
/// </summary>
public sealed class AdminBadgeCountsCacheService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    private readonly IAdminBadgeService _badgeService;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private AdminBadgeCountsModel? _counts;
    private DateTime _cachedAt;

    public string? LastError { get; private set; }

    public AdminBadgeCountsCacheService(IAdminBadgeService badgeService)
    {
        _badgeService = badgeService;
    }

    public void Invalidate() => _counts = null;

    public async Task<AdminBadgeCountsModel?> GetCountsAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _counts != null && DateTime.UtcNow - _cachedAt < CacheDuration)
            return _counts;

        await _gate.WaitAsync();
        try
        {
            if (!forceRefresh && _counts != null && DateTime.UtcNow - _cachedAt < CacheDuration)
                return _counts;

            var result = await _badgeService.GetCounts();
            if (result.Success && result.Data != null)
            {
                _counts = result.Data;
                _cachedAt = DateTime.UtcNow;
                LastError = null;
                return _counts;
            }

            LastError = AdminUiErrorHelper.FromApi(result.Message, "Failed to load badge counts.");
            return _counts;
        }
        finally
        {
            _gate.Release();
        }
    }
}
