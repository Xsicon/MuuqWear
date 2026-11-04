using System.Collections.Concurrent;

namespace MuuqWear.Web.Services;

public sealed class PasswordResetThrottle
{
    public const int EmailLimit = 3;
    public const int ClientLimit = 8;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _hits = new();

    public bool TryConsume(string key, int limit, DateTimeOffset now)
    {
        var bucket = _hits.GetOrAdd(key, _ => new List<DateTimeOffset>());
        lock (bucket)
        {
            var cutoff = now - Window;
            bucket.RemoveAll(hit => hit < cutoff);
            if (bucket.Count >= limit)
                return false;
            bucket.Add(now);
            return true;
        }
    }
}
