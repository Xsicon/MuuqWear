using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.JSInterop;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Authentication;

namespace MuuqWear.Client.Auth;

public sealed class BrowserTokenStore
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJSRuntime _js;

    public BrowserTokenStore(IHttpClientFactory httpClientFactory, IJSRuntime js)
    {
        _httpClientFactory = httpClientFactory;
        _js = js;
    }

    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }

    public async Task SaveAsync(AuthResponseModel session)
    {
        AccessToken = session.AccessToken;
        RefreshToken = session.RefreshToken;
        try
        {
            await _js.InvokeVoidAsync("mwTokenStore.setRefresh", session.RefreshToken);
        }
        catch (JSException) { }
        catch (InvalidOperationException) { }
    }

    public async Task RestoreAsync()
    {
        try
        {
            RefreshToken = await _js.InvokeAsync<string?>("mwTokenStore.getRefresh");
        }
        catch (JSException)
        {
            return;
        }
        catch (InvalidOperationException)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(RefreshToken))
            await TryRefreshAsync();
    }

    public async Task<bool> TryRefreshAsync()
    {
        if (string.IsNullOrWhiteSpace(RefreshToken))
            return false;

        try
        {
            var client = _httpClientFactory.CreateClient("MuuqWearApiRaw");
            var response = await client.PostAsJsonAsync(
                "api/Auth/refresh-token",
                new RefreshTokenBody(RefreshToken));
            var body = await response.Content.ReadFromJsonAsync<Response<AuthResponseModel>>();
            if (body?.Success != true || body.Data?.AccessToken is null)
                return false;

            AccessToken = body.Data.AccessToken;
            if (!string.IsNullOrWhiteSpace(body.Data.RefreshToken))
                await SaveAsync(body.Data);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed record RefreshTokenBody(string RefreshToken);
}
