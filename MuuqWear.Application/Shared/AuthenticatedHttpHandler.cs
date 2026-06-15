using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MuuqWear.Application.Services.AuthService;
using MuuqWear.Model.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace MuuqWear.Application.Shared;

public class AuthenticatedHttpHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CustomAuthenticationStateProvider _authStateProvider;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly AuthSessionService _authSession;

    public AuthenticatedHttpHandler(
        IHttpContextAccessor httpContextAccessor,
        CustomAuthenticationStateProvider authStateProvider,
        IConfiguration configuration,
        IMemoryCache cache,
        AuthSessionService authSession)
    {
        _httpContextAccessor = httpContextAccessor;
        _authStateProvider = authStateProvider;
        _configuration = configuration;
        _cache = cache;
        _authSession = authSession;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        var token = await GetTokenAsync(context);

        if (!string.IsNullOrEmpty(token) && IsTokenExpired(token))
        {
            var newToken = await TryRefreshAsync(context);
            if (newToken != null)
                token = newToken;
        }

        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
            !string.IsNullOrEmpty(token))
        {
            var newToken = await TryRefreshAsync(context);
            if (newToken != null)
            {
                var retryRequest = await CloneRequest(request);
                retryRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", newToken);
                return await base.SendAsync(retryRequest, cancellationToken);
            }

            await HandleSignOut(context);
        }

        return response;
    }

    private async Task HandleSignOut(HttpContext? context)
    {
        _authSession.MarkExpired();

        if (context != null && !context.Response.HasStarted)
        {
            await context.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Redirect("/login?expired=true");
        }
    }

    private bool IsTokenExpired(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.ValidTo < DateTime.UtcNow.AddSeconds(30);
        }
        catch
        {
            return false;
        }
    }

    private async Task<string?> GetTokenAsync(HttpContext? context)
    {
        var user = await ResolveUserAsync(context);
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var userId = user.FindFirst("UserId")?.Value;

        if (!string.IsNullOrEmpty(userId) &&
            _cache.TryGetValue($"access_token_{userId}", out string? cached))
            return cached;

        return user.FindFirst("AccessToken")?.Value;
    }

    private async Task<ClaimsPrincipal?> ResolveUserAsync(HttpContext? context)
    {
        if (context?.User?.Identity?.IsAuthenticated == true)
            return context.User;

        var cached = await _authStateProvider.GetAuthenticationStateAsync();
        if (cached.User.Identity?.IsAuthenticated == true)
            return cached.User;

        if (!string.IsNullOrEmpty(_authStateProvider.CurrentUser.AccessToken))
        {
            return (await _authStateProvider.GetAuthenticationStateAsync()).User;
        }

        return null;
    }

    private async Task<string?> TryRefreshAsync(HttpContext? context)
    {
        var user = await ResolveUserAsync(context);
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var refreshToken = user.FindFirst("RefreshToken")?.Value;
        var userId = user.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(refreshToken))
            return null;

        if (!string.IsNullOrEmpty(userId) &&
            _cache.TryGetValue($"access_token_{userId}", out string? cached) &&
            !IsTokenExpired(cached!))
            return cached;

        try
        {
            var apiBaseUrl = _configuration["ApiBaseUrl"];
            using var http = new HttpClient();

            var result = await http.PostAsJsonAsync(
                $"{apiBaseUrl}api/Auth/refresh-token",
                new { refreshToken });

            if (!result.IsSuccessStatusCode)
                return null;

            var response = await result.Content
                .ReadFromJsonAsync<Response<AuthResponseModel>>();

            if (response?.Success != true || response.Data == null)
                return null;

            _cache.Set($"access_token_{userId}",
                response.Data.AccessToken,
                TimeSpan.FromMinutes(1.5));

            if (context != null)
                await UpdateCookieAsync(context, response.Data);

            return response.Data.AccessToken;
        }
        catch
        {
            return null;
        }
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
