using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MuuqWear.Application.Shared;

public sealed class HmacAuthTicketFormat : ISecureDataFormat<AuthenticationTicket>
{
    private readonly StaffSessionJwt _jwt;

    public HmacAuthTicketFormat(StaffSessionJwt jwt)
    {
        _jwt = jwt;
    }

    public string Protect(AuthenticationTicket data) => Protect(data, null);

    public string Protect(AuthenticationTicket data, string? purpose)
    {
        var uid = data.Principal.FindFirst("UserId")?.Value ?? "";
        var email = data.Principal.FindFirst(ClaimTypes.Email)?.Value ?? "";
        var role = MuuqWear.Web.Helpers.AdminPortalUserContext.GetRole(data.Principal) ?? "user";
        var name = data.Principal.FindFirst(ClaimTypes.Name)?.Value;
        return _jwt.Create(uid, email, role, name);
    }

    public AuthenticationTicket? Unprotect(string? protectedText) => Unprotect(protectedText, null);

    public AuthenticationTicket? Unprotect(string? protectedText, string? purpose)
    {
        var principal = _jwt.Validate(protectedText);
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        var identity = new ClaimsIdentity(
            principal.Claims,
            CookieAuthenticationDefaults.AuthenticationScheme,
            ClaimTypes.Name,
            ClaimTypes.Role);
        var role = MuuqWear.Web.Helpers.AdminPortalUserContext.GetRole(new ClaimsPrincipal(identity));
        if (!string.IsNullOrEmpty(role) && identity.FindFirst(ClaimTypes.Role) is null)
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        if (string.IsNullOrEmpty(identity.FindFirst("AccessToken")?.Value) && !string.IsNullOrWhiteSpace(protectedText))
            identity.AddClaim(new Claim("AccessToken", protectedText));

        return new AuthenticationTicket(
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
            },
            CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
