using System.Text.Json;
using Microsoft.JSInterop;
using MuuqWear.Web.Helpers;

namespace MuuqWear.Web.Services;

/// <summary>
/// Persists admin header dismissed/read state in local storage, scoped per user.
/// </summary>
public sealed class AdminHeaderReadStateStore
{
    private const string NotificationIdsKey = "admin-read-notification-ids";
    private const string NoteReadAtKey = "admin-read-note-at";
    private const string LowStockFirstSeenKey = "admin-low-stock-first-seen";

    private readonly IJSRuntime _js;
    private readonly string _userScope;

    public AdminHeaderReadStateStore(IJSRuntime js, string? userId)
    {
        _js = js;
        _userScope = string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId.Trim();
    }

    public async Task<HashSet<Guid>> LoadNotificationIdsAsync()
    {
        var ids = await LoadJsonAsync<List<Guid>>(NotificationIdsKey);
        return ids is { Count: > 0 } ? ids.ToHashSet() : new HashSet<Guid>();
    }

    public Task SaveNotificationIdsAsync(IEnumerable<Guid> ids) =>
        SaveJsonAsync(NotificationIdsKey, ids.Distinct().ToList());

    public async Task<Dictionary<Guid, DateTime>> LoadNoteReadAtAsync()
    {
        var entries = await LoadJsonAsync<List<NoteReadAtEntry>>(NoteReadAtKey);
        if (entries is not { Count: > 0 })
            return new Dictionary<Guid, DateTime>();

        return entries.ToDictionary(
            entry => entry.CustomerId,
            entry => AdminDateTimeHelper.ToUtc(entry.ReadAt));
    }

    public Task SaveNoteReadAtAsync(IReadOnlyDictionary<Guid, DateTime> readAtByCustomerId) =>
        SaveJsonAsync(
            NoteReadAtKey,
            readAtByCustomerId
                .Select(pair => new NoteReadAtEntry
                {
                    CustomerId = pair.Key,
                    ReadAt = AdminDateTimeHelper.ToUtc(pair.Value)
                })
                .ToList());

    public async Task<Dictionary<Guid, DateTime>> LoadLowStockFirstSeenAsync()
    {
        var entries = await LoadJsonAsync<List<LowStockFirstSeenEntry>>(LowStockFirstSeenKey);
        if (entries is not { Count: > 0 })
            return new Dictionary<Guid, DateTime>();

        return entries.ToDictionary(
            entry => entry.ProductId,
            entry => AdminDateTimeHelper.ToUtc(entry.FirstSeenAt));
    }

    public Task SaveLowStockFirstSeenAsync(IReadOnlyDictionary<Guid, DateTime> firstSeenAt) =>
        SaveJsonAsync(
            LowStockFirstSeenKey,
            firstSeenAt
                .Select(pair => new LowStockFirstSeenEntry
                {
                    ProductId = pair.Key,
                    FirstSeenAt = AdminDateTimeHelper.ToUtc(pair.Value)
                })
                .ToList());

    private string ScopeKey(string key) => $"{key}:{_userScope}";

    private async Task<T?> LoadJsonAsync<T>(string key)
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("adminHeaderReadState.load", ScopeKey(key));
            if (string.IsNullOrWhiteSpace(json))
                return default;

            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }

    private async Task SaveJsonAsync<T>(string key, T value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _js.InvokeVoidAsync("adminHeaderReadState.save", ScopeKey(key), json);
        }
        catch
        {
            // Ignore storage failures; in-memory state still works for the session.
        }
    }

    private sealed class NoteReadAtEntry
    {
        public Guid CustomerId { get; set; }
        public DateTime ReadAt { get; set; }
    }

    private sealed class LowStockFirstSeenEntry
    {
        public Guid ProductId { get; set; }
        public DateTime FirstSeenAt { get; set; }
    }
}
