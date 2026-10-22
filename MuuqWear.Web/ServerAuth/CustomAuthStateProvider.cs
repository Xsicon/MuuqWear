using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Authentication;
using MuuqWear.Web.Helpers;
using System.Security.Claims;

namespace MuuqWear.Application.Services.AuthService;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider, IHostEnvironmentAuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthSessionService _authSession;
    private readonly StaffSessionJwt _staffJwt;
    private readonly HmacAuthTicketFormat _ticketFormat;
    private Task<AuthenticationState>? _hostState;
    private AuthenticationState? _cachedState;
    private bool _sessionExpired;

    public LoggedInUserModel CurrentUser { get; private set; } = new();

    public CustomAuthenticationStateProvider(
        IHttpContextAccessor httpContextAccessor,
        AuthSessionService authSession,
        StaffSessionJwt staffJwt,
        HmacAuthTicketFormat ticketFormat)
    {
        _httpContextAccessor = httpContextAccessor;
        _authSession = authSession;
        _staffJwt = staffJwt;
        _ticketFormat = ticketFormat;
    }

    public void SetAuthenticationState(Task<AuthenticationState> authenticationStateTask)
    {
        _sessionExpired = false;
        _cachedState = null;
        _hostState = authenticationStateTask;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void NotifyLoggedIn(ClaimsPrincipal principal)
    {
        _sessionExpired = false;
        _authSession.Reset();
        var enriched = Enrich(principal);
        _cachedState = new AuthenticationState(enriched);
        _hostState = Task.FromResult(_cachedState);
        CurrentUser = LoggedInUserModel.FromClaimsPrincipal(enriched);
        NotifyAuthenticationStateChanged(Task.FromResult(_cachedState));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_sessionExpired)
            return Anonymous();

        if (_cachedState != null)
            return _cachedState;

        if (_hostState != null)
        {
            var host = await _hostState;
            return Cache(Enrich(host.User));
        }

        var httpUser = _httpContextAccessor.HttpContext?.User;
        if (httpUser?.Identity?.IsAuthenticated == true)
            return Cache(Enrich(httpUser));

        var fromCookie = ReadSessionCookie();
        if (fromCookie != null)
            return Cache(fromCookie);

        return Anonymous();
    }

    public void SyncFromPrincipal(ClaimsPrincipal principal)
    {
        CurrentUser = LoggedInUserModel.FromClaimsPrincipal(Enrich(principal));
    }

    private AuthenticationState Cache(ClaimsPrincipal principal)
    {
        CurrentUser = LoggedInUserModel.FromClaimsPrincipal(principal);
        _cachedState = new AuthenticationState(principal);
        return _cachedState;
    }

    private ClaimsPrincipal Enrich(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            return user;

        if (!string.IsNullOrWhiteSpace(AdminPortalUserContext.GetRole(user)))
            return user;

        var token = user.FindFirst("AccessToken")?.Value;
        var fromJwt = _staffJwt.Validate(token);
        return fromJwt?.Identity?.IsAuthenticated == true ? fromJwt : user;
    }

    private ClaimsPrincipal? ReadSessionCookie()
    {
        var cookie = _httpContextAccessor.HttpContext?.Request.Cookies[CookieAuthenticationDefaults.AuthenticationScheme];
        cookie ??= _httpContextAccessor.HttpContext?.Request.Cookies["__session"];
        if (string.IsNullOrWhiteSpace(cookie))
            return null;

        var ticket = _ticketFormat.Unprotect(cookie);
        return ticket?.Principal?.Identity?.IsAuthenticated == true ? ticket.Principal : null;
    }

    private static AuthenticationState Anonymous() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}
