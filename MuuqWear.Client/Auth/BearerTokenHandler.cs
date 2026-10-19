using System.Net;
using System.Net.Http.Headers;

namespace MuuqWear.Client.Auth;

public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly BrowserTokenStore _tokens;

    public BearerTokenHandler(BrowserTokenStore tokens)
    {
        _tokens = tokens;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var isRefresh = path.Contains("refresh-token", StringComparison.OrdinalIgnoreCase);
        if (!isRefresh)
            AddBearer(request);

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized || isRefresh)
            return response;

        if (!await _tokens.TryRefreshAsync() || _tokens.AccessToken is null)
            return response;

        using var retry = await CloneAsync(request);
        AddBearer(retry);
        return await base.SendAsync(retry, cancellationToken);
    }

    private void AddBearer(HttpRequestMessage request)
    {
        if (string.IsNullOrWhiteSpace(_tokens.AccessToken))
            return;

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (request.Content is null)
            return clone;

        var bytes = await request.Content.ReadAsByteArrayAsync();
        clone.Content = new ByteArrayContent(bytes);
        foreach (var header in request.Content.Headers)
            clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        return clone;
    }
}
