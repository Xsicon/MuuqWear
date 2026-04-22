using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using MuuqWear.Model.Authentication;
using System.Security.Claims;

namespace MuuqWear.Application.Services.AuthService;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private AuthenticationState? _cachedState;

    public LoggedInUserModel CurrentUser { get; private set; } = new();

    public CustomAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            CurrentUser = LoggedInUserModel.FromClaimsPrincipal(httpContext.User);
            _cachedState = new AuthenticationState(httpContext.User);
            return Task.FromResult(_cachedState);
        }

        if (_cachedState != null)
            return Task.FromResult(_cachedState);

        return Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    public void SyncFromPrincipal(ClaimsPrincipal principal)
    {
        CurrentUser = LoggedInUserModel.FromClaimsPrincipal(principal);
    }
}