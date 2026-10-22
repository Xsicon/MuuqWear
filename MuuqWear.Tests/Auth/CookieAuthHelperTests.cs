using MuuqWear.Application.Shared;
using MuuqWear.Model.Authentication;
using Xunit;

namespace MuuqWear.Tests.Auth;

public class CookieAuthHelperTests
{
    [Fact]
    public void SessionOmitsFirebaseTokens()
    {
        var session = CookieAuthHelper.FromAuthResponse(
            new AuthResponseModel
            {
                AccessToken = "short.hmac.jwt",
                RefreshToken = null,
                Email = "info@kobneti.com",
                UserId = "uid-1",
                UserName = "Ops",
                Role = "admin"
            },
            "/admin",
            false);

        Assert.Equal("short.hmac.jwt", session.Token);
        Assert.True(string.IsNullOrEmpty(session.RefreshToken));
        var principal = CookieAuthHelper.CreatePrincipal(session);
        Assert.Null(principal.FindFirst("RefreshToken"));
        Assert.Equal("short.hmac.jwt", principal.FindFirst("AccessToken")?.Value);
        Assert.Equal("admin", principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value);
    }
}
