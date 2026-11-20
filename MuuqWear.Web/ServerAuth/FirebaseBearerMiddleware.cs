using System.Security.Claims;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MuuqWear.Web.ServerAuth;

public sealed class FirebaseBearerMiddleware
{
    private readonly RequestDelegate _next;

    public FirebaseBearerMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, MuuqWear.Application.Shared.StaffSessionJwt staffJwt)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = header["Bearer ".Length..].Trim();
            if (!string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                    var email = Claim(decoded, "email");
                    var name = Claim(decoded, "name");
                    var secondFactor = StaffMfaPolicy.FirebaseHasSecondFactor(decoded.Claims);
                    var role = CookieSessionVerifier.RoleFor(decoded.Claims, secondFactor, staffJwt.RequireStaffMfa);
                    List<Claim> claims =
                    [
                        new Claim("UserId", decoded.Uid),
                        new Claim(ClaimTypes.NameIdentifier, decoded.Uid),
                        new Claim(ClaimTypes.Email, email),
                        new Claim(ClaimTypes.Name, string.IsNullOrWhiteSpace(name) ? email : name),
                        new Claim(ClaimTypes.Role, role),
                        new Claim("role", role)
                    ];
                    if (secondFactor)
                        claims.Add(new Claim(StaffMfaPolicy.ClaimType, StaffMfaPolicy.Totp));
                    var identity = new ClaimsIdentity(
                        claims,
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        ClaimTypes.Name,
                        ClaimTypes.Role);
                    context.User = new ClaimsPrincipal(identity);
                }
                catch (FirebaseAuthException)
                {
                }
            }
        }

        await _next(context);
    }

    private static string Claim(FirebaseToken decoded, string key) =>
        decoded.Claims.TryGetValue(key, out var value) ? value?.ToString() ?? "" : "";
}
