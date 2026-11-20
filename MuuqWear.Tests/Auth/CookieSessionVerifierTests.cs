using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using MuuqWear.Application.Shared;
using MuuqWear.Web.ServerAuth;
using Xunit;

namespace MuuqWear.Tests.Auth;

public class CookieSessionVerifierTests
{
    [Fact]
    public async Task ForgedAdminJsonIsRejectedWithoutARealToken()
    {
        var verifier = Create();
        var session = await verifier.CreateAsync("not-a-token", "/admin", true);

        Assert.Null(session);
    }

    [Fact]
    public async Task StaffJwtWinsAndIgnoresClientRoleAndUserId()
    {
        var (verifier, jwt) = CreateWithJwt();
        var token = jwt.Create("staff-uid", "ops@muuqwear.com", "admin", "Ops", "totp");

        var session = await verifier.CreateAsync(token, "https://evil.example/phish", true);

        Assert.NotNull(session);
        Assert.Equal("staff-uid", session!.UserId);
        Assert.Equal("admin", session.Role);
        Assert.Equal("ops@muuqwear.com", session.Email);
        Assert.Equal("/admin", session.ReturnUrl);
        Assert.Equal("totp", session.Mfa);
    }

    [Fact]
    public async Task StaffJwtWithoutSecondFactorIsRejected()
    {
        var (verifier, jwt) = CreateWithJwt();
        var token = jwt.Create("staff-uid", "ops@muuqwear.com", "admin", "Ops");

        Assert.Null(await verifier.CreateAsync(token, "/admin", true));
    }

    [Fact]
    public async Task FirebaseAdminWithoutSecondFactorGetsShopperAccessOnly()
    {
        var firebase = new StubFirebaseVerifier(new FirebaseIdTokenClaims(
            "admin-uid",
            "info@kobneti.com",
            "Ops",
            new Dictionary<string, object>
            {
                ["admin"] = true,
                ["firebase"] = new Dictionary<string, object> { ["sign_in_provider"] = "password" }
            }));
        var verifier = Create(firebase);

        var session = await verifier.CreateAsync("verified-id-token", "/admin/products", true);

        Assert.NotNull(session);
        Assert.Equal("user", session!.Role);
        Assert.Equal("/profile", session.ReturnUrl);
        Assert.Null(session.Mfa);
    }

    [Fact]
    public async Task FirebaseClaimsSupplyUidAndRoleNotTheClientBody()
    {
        var firebase = new StubFirebaseVerifier(new FirebaseIdTokenClaims(
            "firebase-uid",
            "shopper@muuqwear.com",
            "Shopper",
            new Dictionary<string, object>()));
        var verifier = Create(firebase);

        var session = await verifier.CreateAsync("verified-id-token", "/admin", true);

        Assert.NotNull(session);
        Assert.Equal("firebase-uid", session!.UserId);
        Assert.Equal("user", session.Role);
        Assert.Equal("/profile", session.ReturnUrl);
        Assert.Equal("shopper@muuqwear.com", session.Email);
    }

    [Fact]
    public async Task FirebaseAdminCustomClaimMapsToStaff()
    {
        var firebase = new StubFirebaseVerifier(new FirebaseIdTokenClaims(
            "admin-uid",
            "info@kobneti.com",
            "Ops",
            new Dictionary<string, object>
            {
                ["admin"] = true,
                ["firebase"] = new Dictionary<string, object> { ["sign_in_second_factor"] = "totp" }
            }));
        var verifier = Create(firebase);

        var session = await verifier.CreateAsync("verified-id-token", "/admin/products", true);

        Assert.NotNull(session);
        Assert.Equal("totp", session!.Mfa);
        Assert.Equal("admin", session!.Role);
        Assert.Equal("admin-uid", session.UserId);
        Assert.Equal("/admin/products", session.ReturnUrl);
    }

    [Fact]
    public void ShopperCallerRequiresAuthenticatedUserId()
    {
        var anon = new ClaimsPrincipal(new ClaimsIdentity());
        Assert.Null(VerifiedAuthSession.Shopper(anon).Uid);

        var emailOnly = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, "victim@muuqwear.com")],
            "Cookies"));
        Assert.Null(VerifiedAuthSession.Shopper(emailOnly).Uid);

        var shopper = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("UserId", "uid-1"),
                new Claim(ClaimTypes.Email, "a@b.com")
            ],
            "Cookies"));
        Assert.Equal("uid-1", VerifiedAuthSession.Shopper(shopper).Uid);
    }

    [Fact]
    public void RoleFromClaimsIgnoresUnknownStaffNames()
    {
        var role = VerifiedAuthSession.RoleFromClaims(new Dictionary<string, object>
        {
            ["role"] = "superadmin"
        });
        Assert.Equal("user", role);
    }

    private static CookieSessionVerifier Create(IFirebaseIdTokenVerifier? firebase = null) =>
        CreateWithJwt(firebase).Verifier;

    private static (CookieSessionVerifier Verifier, StaffSessionJwt Jwt) CreateWithJwt(
        IFirebaseIdTokenVerifier? firebase = null)
    {
        var jwt = new StaffSessionJwt(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:JwtSecret"] = "local-dev-jwt-secret-not-for-production-32"
            })
            .Build());
        return (new CookieSessionVerifier(jwt, firebase ?? new StubFirebaseVerifier(null)), jwt);
    }

    private sealed class StubFirebaseVerifier : IFirebaseIdTokenVerifier
    {
        private readonly FirebaseIdTokenClaims? _claims;

        public StubFirebaseVerifier(FirebaseIdTokenClaims? claims) => _claims = claims;

        public Task<FirebaseIdTokenClaims?> VerifyAsync(string idToken, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.IsNullOrWhiteSpace(idToken) ? null : _claims);
    }
}
