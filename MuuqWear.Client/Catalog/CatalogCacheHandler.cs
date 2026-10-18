using System.Net;
using System.Text;
using Microsoft.JSInterop;

namespace MuuqWear.Client.Catalog;

public sealed class CatalogCacheHandler : DelegatingHandler
{
    private static readonly TimeSpan FreshFor = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan KeepFor = TimeSpan.FromHours(6);
    private static readonly HashSet<string> InFlight = new(StringComparer.Ordinal);

    private readonly IJSRuntime _js;

    public CatalogCacheHandler(IJSRuntime js)
    {
        _js = js;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Get || !IsCatalogGet(request.RequestUri))
            return await base.SendAsync(request, cancellationToken);

        var key = request.RequestUri!.PathAndQuery;
        var cached = await ReadAsync(key);
        if (cached?.Body is { Length: > 0 })
        {
            var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(cached.SavedAt);
            if (age < KeepFor)
            {
                if (age >= FreshFor)
                    RefreshInBackground(request.RequestUri, key);

                return ToResponse(cached);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);
        await StoreAsync(response, key, cancellationToken);
        return response;
    }

    private void RefreshInBackground(Uri uri, string key)
    {
        lock (InFlight)
        {
            if (!InFlight.Add(key))
                return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
                using var response = await client.GetAsync(uri);
                await StoreAsync(response, key, CancellationToken.None);
            }
            catch
            {
                // Keep the stored copy when refresh fails.
            }
            finally
            {
                lock (InFlight)
                    InFlight.Remove(key);
            }
        });
    }

    private async Task<CatalogCacheEntry?> ReadAsync(string key)
    {
        try
        {
            return await _js.InvokeAsync<CatalogCacheEntry?>("mwCatalogCache.get", key);
        }
        catch
        {
            return null;
        }
    }

    private async Task StoreAsync(HttpResponseMessage response, string key, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode || response.Content is null)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? "application/json";
        response.Content = new StringContent(body, Encoding.UTF8, mediaType);

        if (body.Length is 0 or > 180_000)
            return;
        if (body.Contains("\"success\":false", StringComparison.OrdinalIgnoreCase)
            || body.Contains("\"success\": false", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            await _js.InvokeVoidAsync("mwCatalogCache.set", cancellationToken, key, body, mediaType);
        }
        catch
        {
            // Storage can be full or blocked. The response is still returned.
        }
    }

    private static bool IsCatalogGet(Uri? uri)
    {
        if (uri is null)
            return false;

        var path = uri.AbsolutePath;
        return path.StartsWith("/api/Product", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/Category", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/Journal", StringComparison.OrdinalIgnoreCase);
    }

    private static HttpResponseMessage ToResponse(CatalogCacheEntry entry)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(entry.Body!, Encoding.UTF8, entry.ContentType ?? "application/json")
        };
        return response;
    }

    private sealed class CatalogCacheEntry
    {
        public long SavedAt { get; set; }
        public string? Body { get; set; }
        public string? ContentType { get; set; }
    }
}
