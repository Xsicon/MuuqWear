using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MuuqWear.Model.Authentication;
using MuuqWear.Web.Constants;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace MuuqWear.Application.Shared;
public class TokenRefreshMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly AuthSessionService _authSession; // ← add
    private readonly ILogger<TokenRefreshMiddleware> _logger;
    private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);


    public TokenRefreshMiddleware(RequestDelegate next, IConfiguration configuration, IMemoryCache cache,
         AuthSessionService authSession, ILogger<TokenRefreshMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _cache = cache;
        _authSession = authSession;
        _logger = logger;
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
                .ContainsKey("__session");

            if (path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Admin request is unauthenticated. hadCookie={HadCookie} path={Path}",
                    hadCookie,
                    path);
            }

            if (hadCookie)
                _authSession.NotifySessionExpired();
            context.Response.Cookies.Delete("__session");


            await _next(context);
            return;
        }

        // user is authenticated → check JWT expiry
        var accessToken = context.User.FindFirst("AccessToken")?.Value;
        var refreshToken = context.User.FindFirst("RefreshToken")?.Value;
        var userId = context.User.FindFirst("UserId")?.Value;
        var role = MuuqWear.Web.Helpers.AdminPortalUserContext.GetRole(context.User);
        var staffSession = AdminPortalRoles.IsAllowed(role);

        if (!string.IsNullOrEmpty(accessToken) && IsTokenExpired(accessToken))
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                await context.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
                context.Response.Cookies.Append("session_message",
                    "Your session has expired. Please sign in again.");
                context.Response.Redirect(staffSession
                    ? "/admin/f8e2a947-3c1b-4d9e-b562-7a0f1e8d3c92/login"
                    : "/login?expired=true");
                return;
            }

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
        await _next(context);
    }

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
            return false;
        }
        try
        {
            var firebase = await TryFirebaseRefreshAsync(refreshToken);
            if (firebase is null)
                return false;
            _cache.Set($"access_token_{userId}", firebase.AccessToken, TimeSpan.FromMinutes(50));
            await RewriteCookieAsync(context, firebase);
            return true;
        }
        catch (Exception ex)
        {
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
        if (!string.IsNullOrEmpty(data.Role))
            ReplaceOrAdd(identity, ClaimTypes.Role, data.Role);

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

    private static readonly HttpClient AccountStatusClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    private async Task<AuthResponseModel?> TryFirebaseRefreshAsync(string refreshToken)
    {
        var apiKey = _configuration["Firebase:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        try
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            });
            using var result = await AccountStatusClient.PostAsync(
                $"https://securetoken.googleapis.com/v1/token?key={Uri.EscapeDataString(apiKey)}",
                content);
            if (!result.IsSuccessStatusCode)
                return null;

            var payload = await result.Content.ReadFromJsonAsync<FirebaseRefreshResponse>();
            if (string.IsNullOrWhiteSpace(payload?.IdToken))
                return null;

            return new AuthResponseModel
            {
                AccessToken = payload.IdToken,
                RefreshToken = payload.RefreshToken ?? refreshToken,
                UserId = payload.UserId,
                Role = FirebaseIdTokenRole.FromAccessToken(payload.IdToken)
            };
        }
        catch
        {
            return null;
        }
    }

    private sealed class FirebaseRefreshResponse
    {
        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }
    }
}
