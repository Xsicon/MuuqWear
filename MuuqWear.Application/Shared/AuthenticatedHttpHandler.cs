using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MuuqWear.Application.Services.AuthService;
using MuuqWear.Model.Authentication;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace MuuqWear.Application.Shared;
public class AuthenticatedHttpHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly NavigationManager _navigationManager;
    private readonly AuthSessionService _authSession;



    public AuthenticatedHttpHandler(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        IMemoryCache cache, NavigationManager navigationManager, AuthSessionService authSession)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _cache = cache;
        _authSession = authSession;
        _navigationManager = navigationManager;


    }

    protected override async Task<HttpResponseMessage> SendAsync(
     HttpRequestMessage request,
     CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        var token = GetToken(context);

        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken);

        // ✅ everything inside this block
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var newToken = await TryRefreshAsync(context);
            if (newToken != null)
            {
                var retryRequest = await CloneRequest(request);
                retryRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", newToken);
                return await base.SendAsync(retryRequest, cancellationToken);
            }

            // ✅ only runs when 401 AND refresh failed
            if (context != null && !context.Response.HasStarted)
            {
                await context.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
                context.Response.Redirect("/login?expired=true");
            }
            else
            {
                Console.WriteLine("MarkExpired called — session expired");
                _authSession.MarkExpired();
            }
        }

        return response;
    }
    private string? GetToken(HttpContext? context)
    {
        if (context == null) return null;
        var userId = context.User.FindFirst("UserId")?.Value;

        if (!string.IsNullOrEmpty(userId) &&
            _cache.TryGetValue($"access_token_{userId}", out string? cached))
            return cached;

        return context.User.FindFirst("AccessToken")?.Value;
    }

    private async Task<string?> TryRefreshAsync(HttpContext? context)
    {
        if (context == null) return null;

        var refreshToken = context.User.FindFirst("RefreshToken")?.Value;
        var userId = context.User.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(refreshToken)) return null;

        // check cache first — maybe middleware already refreshed
        if (!string.IsNullOrEmpty(userId) &&
            _cache.TryGetValue($"access_token_{userId}", out string? cached))
            return cached;

        try
        {
            var apiBaseUrl = _configuration["ApiBaseUrl"];
            using var http = new HttpClient();

            var result = await http.PostAsJsonAsync(
                $"{apiBaseUrl}api/Auth/refresh-token",
                new { refreshToken });

            if (!result.IsSuccessStatusCode) return null;

            var response = await result.Content
                .ReadFromJsonAsync<Response<AuthResponseModel>>();

            if (response?.Success != true || response.Data == null) return null;

            // cache new token
            _cache.Set($"access_token_{userId}",
                response.Data.AccessToken,
                TimeSpan.FromMinutes(1.5));

            // update cookie for next requests
            await UpdateCookieAsync(context, response.Data);

            return response.Data.AccessToken;
        }
        catch { return null; }
    }

    private async Task UpdateCookieAsync(HttpContext context, AuthResponseModel data)
    {
        try
        {
            var identity = (ClaimsIdentity)context.User.Identity!;
            ReplaceOrAdd(identity, "AccessToken", data.AccessToken!);
            ReplaceOrAdd(identity, "RefreshToken", data.RefreshToken!);

            var props = await context.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                context.User,
                props?.Properties ?? new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                });
        }
        catch { }
    }

    private void ReplaceOrAdd(ClaimsIdentity identity, string type, string value)
    {
        var existing = identity.FindFirst(type);
        if (existing != null) identity.RemoveClaim(existing);
        identity.AddClaim(new Claim(type, value));
    }

    private async Task<HttpRequestMessage> CloneRequest(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        if (original.Content != null)
        {
            var bytes = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }
}