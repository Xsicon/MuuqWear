using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using MuuqWear.Model.Authentication;
using System.Security.Claims;

namespace MuuqWear.Application.Services.AuthService;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private AuthenticationState? _cachedState;
    private bool _sessionExpired = false; // ← add this

    public LoggedInUserModel CurrentUser { get; private set; } = new();

    public CustomAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    //public void NotifySessionExpired()
    //{
    //    _sessionExpired = true;
    //    _cachedState = null; // ← clear so it doesn't return old state
    //    var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
    //    NotifyAuthenticationStateChanged(
    //        Task.FromResult(new AuthenticationState(anonymous)));
    //}

    //  call this on fresh login to reset the flag
    public void NotifyLoggedIn(ClaimsPrincipal principal)
    {
        _sessionExpired = false;
        _cachedState = new AuthenticationState(principal);
        CurrentUser = LoggedInUserModel.FromClaimsPrincipal(principal);
        NotifyAuthenticationStateChanged(Task.FromResult(_cachedState));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        //  session expired → always anonymous
        if (_sessionExpired)
            return Task.FromResult(
                new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

        //  return cache FIRST — covers all SignalR calls where HttpContext = null
        if (_cachedState != null)
            return Task.FromResult(_cachedState);

        // only runs on real HTTP request (first page load)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            CurrentUser = LoggedInUserModel.FromClaimsPrincipal(httpContext.User);
            _cachedState = new AuthenticationState(httpContext.User);
            return Task.FromResult(_cachedState);
        }

        return Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
    public void SyncFromPrincipal(ClaimsPrincipal principal)
    {
        CurrentUser = LoggedInUserModel.FromClaimsPrincipal(principal);
    }
}