using MuuqWear.Web.Constants;
using MuuqWear.Web.Services;
using Xunit;

namespace MuuqWear.Tests.Admin;

public class StaffUserRulesTests
{
    [Fact]
    public void Admin_claim_is_staff()
    {
        var claims = new Dictionary<string, object> { ["admin"] = true };
        Assert.True(StaffUserRules.IsStaff(claims));
        Assert.Equal(AdminPortalRoles.Admin, StaffUserRules.ReadRole(claims));
    }

    [Fact]
    public void Shopper_is_not_staff() =>
        Assert.False(StaffUserRules.IsStaff(new Dictionary<string, object> { ["role"] = "user" }));

    [Fact]
    public void ClaimsFor_admin_sets_admin_flag()
    {
        var claims = StaffUserRules.ClaimsFor(AdminPortalRoles.Admin);
        Assert.True((bool)claims["admin"]);
        Assert.Equal(AdminPortalRoles.Admin, claims["role"]);
    }

    [Fact]
    public void Cannot_change_self() =>
        Assert.NotNull(StaffUserRules.GuardSelf("uid-1", "uid-1"));

    [Fact]
    public void Cannot_remove_last_admin() =>
        Assert.NotNull(StaffUserRules.GuardLastAdmin(true, true, 1));

    [Fact]
    public void Can_demote_when_another_admin_exists() =>
        Assert.Null(StaffUserRules.GuardLastAdmin(true, true, 2));
}
