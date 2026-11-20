using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Authentication;
using Xunit;

namespace MuuqWear.Tests.Auth;

public class StaffSessionJwtTests
{
    [Fact]
    public void CookieTokenStaysSmallAndCarriesAdminRole()
    {
        var jwt = new StaffSessionJwt(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:JwtSecret"] = "local-dev-jwt-secret-not-for-production-32"
            })
            .Build());

        var token = jwt.Create("firebase-uid-abc", "info@kobneti.com", "admin", "Ops");

        Assert.True(token.Length < 800);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("admin", parsed.Claims.First(c => c.Type == StaffSessionJwt.RoleClaimType).Value);
        Assert.Equal("admin", parsed.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal("firebase-uid-abc", parsed.Claims.First(c => c.Type == "UserId").Value);
        Assert.True(Guid.TryParse(parsed.Subject, out _));
    }

    [Fact]
    public void TicketRoundTripsOnASharedSecret()
    {
        var jwt = new StaffSessionJwt(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:JwtSecret"] = "local-dev-jwt-secret-not-for-production-32"
            })
            .Build());
        var format = new HmacAuthTicketFormat(jwt);
        var session = CookieAuthHelper.FromAuthResponse(
            new AuthResponseModel
            {
                AccessToken = jwt.Create("uid-1", "info@kobneti.com", "admin", "Ops"),
                Email = "info@kobneti.com",
                UserId = "uid-1",
                UserName = "Ops",
                Role = "admin"
            },
            "/admin",
            true);
        session.Mfa = "totp";
        var ticket = format.Protect(new AuthenticationTicket(
            CookieAuthHelper.CreatePrincipal(session),
            CookieAuthenticationDefaults.AuthenticationScheme));
        var restored = format.Unprotect(ticket);
        Assert.NotNull(restored);
        Assert.Equal("admin", restored!.Principal.FindFirst(ClaimTypes.Role)?.Value);
        Assert.Equal("uid-1", restored.Principal.FindFirst("UserId")?.Value);
    }

    [Fact]
    public void ValidateExposesRoleAsClaimTypesRole()
    {
        var jwt = new StaffSessionJwt(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:JwtSecret"] = "local-dev-jwt-secret-not-for-production-32"
            })
            .Build());

        var principal = jwt.Validate(jwt.Create("uid-1", "info@kobneti.com", "admin", "Ops", "totp"));

        Assert.NotNull(principal);
        Assert.Equal("admin", principal!.FindFirst(ClaimTypes.Role)?.Value);
        Assert.Equal("admin", principal.FindFirst("app_role")?.Value);
        Assert.True(principal.IsInRole("admin"));
        Assert.Equal("totp", principal.FindFirst("mfa")?.Value);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("support_team")]
    public void StaffTokenWithoutSecondFactorIsDowngradedToShopper(string role)
    {
        var jwt = Jwt();

        var principal = jwt.Validate(jwt.Create("uid-1", "ops@muuqwear.com", role, "Ops"));

        Assert.NotNull(principal);
        Assert.False(principal!.IsInRole(role));
        Assert.Equal("user", principal.FindFirst("app_role")?.Value);
        Assert.Equal("user", principal.FindFirst(ClaimTypes.Role)?.Value);
        Assert.DoesNotContain(principal.Claims, c => c.Type == "role" && c.Value == role);
    }

    [Fact]
    public void OldAdminCookieWithoutSecondFactorLosesAdminAccess()
    {
        var jwt = Jwt();
        var format = new HmacAuthTicketFormat(jwt);

        var restored = format.Unprotect(jwt.Create("uid-1", "ops@muuqwear.com", "admin", "Ops"));

        Assert.NotNull(restored);
        Assert.Equal("user", MuuqWear.Web.Helpers.AdminPortalUserContext.GetRole(restored!.Principal));
    }

    [Fact]
    public void FlagOffKeepsPasswordOnlyStaffSessions()
    {
        var jwt = Jwt(requireMfa: false);

        var principal = jwt.Validate(jwt.Create("uid-1", "ops@muuqwear.com", "admin", "Ops"));

        Assert.True(principal!.IsInRole("admin"));
    }

    private static StaffSessionJwt Jwt(bool? requireMfa = null) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:JwtSecret"] = "local-dev-jwt-secret-not-for-production-32",
                ["Authentication:RequireStaffMfa"] = requireMfa?.ToString()
            })
            .Build());
}
