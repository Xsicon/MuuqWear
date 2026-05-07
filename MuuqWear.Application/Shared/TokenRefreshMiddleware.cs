using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MuuqWear.Model.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;

namespace MuuqWear.Application.Shared;
public class TokenRefreshMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly AuthSessionService _authSession; // ← add
    private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);


    public TokenRefreshMiddleware(RequestDelegate next, IConfiguration configuration, IMemoryCache cache,
         AuthSessionService authSession)
    {
        _next = next;
        _configuration = configuration;
        _cache = cache;
        _authSession = authSession;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        if (path.StartsWith("/_blazor") ||
            path.StartsWith("/_framework") ||
            path.StartsWith("/auth") ||
            Path.HasExtension(path))
        {
            await _next(context);
            return;
        }

        //  cookie gone/expired → notify Blazor circuit
        if (context.User.Identity?.IsAuthenticated == false)
        {
            // check if they HAD a cookie — if cookie exists but invalid = session expired
            var hadCookie = context.Request.Cookies
                .ContainsKey("muuqwear_auth");

            if (hadCookie)
                _authSession.NotifySessionExpired();

            await _next(context);
            return;
        }

        // user is authenticated → check JWT expiry
        var accessToken = context.User.FindFirst("AccessToken")?.Value;
        var refreshToken = context.User.FindFirst("RefreshToken")?.Value;
        var userId = context.User.FindFirst("UserId")?.Value;
        var isActive = await CheckIsActive(accessToken);
        if (!isActive)
        {
            await context.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Redirect("/");
            return;
        }

        if (!string.IsNullOrEmpty(accessToken) && IsTokenExpired(accessToken))
        {
            await _lock.WaitAsync();
            try
            {
                var cacheKey = $"access_token_{userId}";
                if (_cache.TryGetValue(cacheKey, out string? cachedToken)
                    && !IsTokenExpired(cachedToken!))
                {
                    await _next(context);
                    return;
                }

                var refreshed = await TryRefreshAsync(context, refreshToken, userId);

                if (!refreshed)
                {
                    await context.SignOutAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme);

                    context.Response.Cookies.Append("session_message",
                        "Your session has expired. Please sign in again.");

                    context.Response.Redirect("/login?expired=true");
                    return;
                }
            }
            finally
            {
                _lock.Release();
            }
        }
        _ = UpdateLastActive(userId);

        await _next(context);
    }
    // ... rest unchanged}
    private bool IsTokenExpired(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            // 30 second buffer — refresh slightly before actual expiry
            return jwt.ValidTo < DateTime.UtcNow.AddSeconds(30);
        }
        catch
        {
            return true;
        }
    }

    private async Task<bool> TryRefreshAsync(HttpContext context, string? refreshToken, string? userId)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            System.Diagnostics.Debug.WriteLine("RefreshToken is null or empty ❌");
            return false;
        }
        try
        {
            var apiBaseUrl = _configuration["ApiBaseUrl"];
            System.Diagnostics.Debug.WriteLine($"Calling: {apiBaseUrl}api/Auth/refresh-token");
            System.Diagnostics.Debug.WriteLine($"RefreshToken length: {refreshToken?.Length}");
            System.Diagnostics.Debug.WriteLine($"RefreshToken preview: {refreshToken?[..Math.Min(20, refreshToken.Length)]}...");
            System.Diagnostics.Debug.WriteLine($"RefreshToken: {refreshToken}"); // ← full token
            using var http = new HttpClient();

            var body = new { refreshToken = refreshToken };
            var result = await http.PostAsJsonAsync(
                $"{apiBaseUrl}api/Auth/refresh-token", body);
            System.Diagnostics.Debug.WriteLine($"Backend response: {result.StatusCode}");

            if (!result.IsSuccessStatusCode)
            {
                var error = await result.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Backend error: {error}");
                return false;
            }
            var response = await result.Content
                .ReadFromJsonAsync<Response<AuthResponseModel>>();


            System.Diagnostics.Debug.WriteLine($"Deserialized success: {response?.Success}");
            System.Diagnostics.Debug.WriteLine($"Data null: {response?.Data == null}");
            System.Diagnostics.Debug.WriteLine($"AccessToken null: {response?.Data?.AccessToken == null}");
            if (response?.Success != true || response.Data == null) return false;
            var cacheKey = $"access_token_{userId}";
            _cache.Set(cacheKey, response.Data.AccessToken,
                TimeSpan.FromMinutes(1.5));

            // rewrite cookie with fresh tokens 
            await RewriteCookieAsync(context, response.Data);
            System.Diagnostics.Debug.WriteLine($"New token expires: {GetTokenExpiry(response.Data.AccessToken!)}");

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in TryRefreshAsync: {ex.Message}");
            return false;
        }
    }
    private DateTime GetTokenExpiry(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.ValidTo;
    }

    private async Task RewriteCookieAsync(HttpContext context, AuthResponseModel data)
    {
        // keep existing claims, just replace token claims
        var identity = (ClaimsIdentity)context.User.Identity!;

        ReplaceOrAdd(identity, "AccessToken", data.AccessToken!);
        ReplaceOrAdd(identity, "RefreshToken", data.RefreshToken!);

        var existingProps = await context.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            context.User,
            existingProps?.Properties ?? new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });
    }

    private void ReplaceOrAdd(ClaimsIdentity identity, string type, string value)
    {
        var existing = identity.FindFirst(type);
        if (existing != null) identity.RemoveClaim(existing);
        identity.AddClaim(new Claim(type, value));
    }

    private async Task<bool> CheckIsActive(string? accessToken)
    {
        if (string.IsNullOrEmpty(accessToken)) return true; // not logged in → skip

        try
        {
            var apiBaseUrl = _configuration["ApiBaseUrl"];
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", accessToken);

            var result = await http.GetAsync($"{apiBaseUrl}api/Profile/is-active");

            if (!result.IsSuccessStatusCode) return true; // if check fails → don't block

            var response = await result.Content
                .ReadFromJsonAsync<Response<bool>>();

            return response?.Data != false; // false = deleted → block
        }
        catch
        {
            return true;
        }
    }

    private async Task UpdateLastActive(string? userId)
    {
        if (string.IsNullOrEmpty(userId)) return;

        try
        {
            var apiBaseUrl = _configuration["ApiBaseUrl"];
            using var http = new HttpClient();
            await http.PostAsync(
                $"{apiBaseUrl}api/Profile/last-active/{userId}", null);
        }
        catch
        {
        }
    }
}
