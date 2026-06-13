using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using MuuqWear.Model.Authentication;
using System.Security.Claims;

namespace MuuqWear.Application.Shared;

public static class CookieAuthHelper
{
    public static ClaimsPrincipal CreatePrincipal(CookieAuthSession session)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, session.Username ?? ""),
            new(ClaimTypes.Email, session.Email ?? ""),
            new(ClaimTypes.Role, session.Role ?? "user"),
            new("AccessToken", session.Token ?? ""),
            new("RefreshToken", session.RefreshToken ?? ""),
            new("UserId", session.UserId ?? "")
        };

        var identity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identity);
    }

    public static AuthenticationProperties CreateProperties(CookieAuthSession session) =>
        new()
        {
            IsPersistent = session.RememberMe ?? false,
            ExpiresUtc = session.RememberMe ?? false
                ? DateTimeOffset.UtcNow.AddDays(30)
                : DateTimeOffset.UtcNow.AddHours(5)
        };

    public static CookieAuthSession FromAuthResponse(
        AuthResponseModel data,
        string returnUrl,
        bool rememberMe) =>
        new()
        {
            Token = data.AccessToken ?? "",
            RefreshToken = data.RefreshToken ?? "",
            Username = data.UserName ?? "",
            Email = data.Email ?? "",
            Role = data.Role ?? "user",
            UserId = data.UserId ?? "",
            ReturnUrl = returnUrl,
            RememberMe = rememberMe
        };
}
