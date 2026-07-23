using System.Security.Claims;
using MuuqWear.Web.Helpers;
using Xunit;

namespace MuuqWear.Tests.Admin;

public class AdminHeaderUserScopeTests
{
    [Fact]
    public void GetUserId_uses_custom_user_id_claim()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("UserId", "abc-123")
        ]));

        Assert.Equal("abc-123", AdminHeaderUserScope.GetUserId(user));
    }

    [Fact]
    public void GetUserId_returns_empty_when_claim_missing()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Equal(string.Empty, AdminHeaderUserScope.GetUserId(user));
    }
}
