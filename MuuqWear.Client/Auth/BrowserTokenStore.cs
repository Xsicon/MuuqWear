using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using MuuqWear.Model.Authentication;

namespace MuuqWear.Client.Auth;

public sealed class BrowserTokenStore
{
    private readonly IJSRuntime _js;
    private readonly IConfiguration _config;

    public BrowserTokenStore(IJSRuntime js, IConfiguration config)
    {
        _js = js;
        _config = config;
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

        if (!string.IsNullOrWhiteSpace(RefreshToken) && RefreshToken != "firebase")
            await TryRefreshAsync();
    }

    public async Task<bool> TryRefreshAsync()
    {
        if (string.IsNullOrWhiteSpace(RefreshToken) || RefreshToken == "firebase")
            return false;

        var apiKey = _config["Firebase:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return false;

        try
        {
            using var client = new HttpClient();
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = RefreshToken
            });
            using var response = await client.PostAsync(
                $"https://securetoken.googleapis.com/v1/token?key={Uri.EscapeDataString(apiKey)}",
                content);
            if (!response.IsSuccessStatusCode)
                return false;

            var body = await response.Content.ReadAsStringAsync();
            var id = JsonValue(body, "id_token");
            var refresh = JsonValue(body, "refresh_token");
            if (string.IsNullOrWhiteSpace(id))
                return false;

            AccessToken = id;
            if (!string.IsNullOrWhiteSpace(refresh))
                RefreshToken = refresh;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? JsonValue(string json, string key)
    {
        var needle = $"\"{key}\":\"";
        var start = json.IndexOf(needle, StringComparison.Ordinal);
        if (start < 0) return null;
        start += needle.Length;
        var end = json.IndexOf('"', start);
        return end < 0 ? null : json[start..end];
    }
}
