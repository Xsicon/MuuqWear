using MuuqWear.Web.Constants;
using MuuqWear.Web.ServerAuth;
using Xunit;

namespace MuuqWear.Tests.Auth;

public class HostRoutingTests
{
    private const string PublicHost = "muuqwear.com";
    private const string AdminHost = "admin.muuqwear.com";

    [Theory]
    [InlineData("muuqwear.com", "/")]
    [InlineData("muuqwear.com", "/admin")]
    [InlineData("admin.muuqwear.com", "/")]
    [InlineData("admin.muuqwear.com", "/shop")]
    [InlineData("", "/admin/orders")]
    public void Empty_admin_host_never_redirects(string host, string path)
    {
        Assert.Null(HostRouting.RedirectFor(host, path, PublicHost, ""));
        Assert.Null(HostRouting.RedirectFor(host, path, PublicHost, "   "));
    }

    [Fact]
    public void Admin_root_goes_to_the_staff_login_path()
    {
        Assert.Equal(
            AdminPortalPaths.Login,
            HostRouting.RedirectFor(AdminHost, "/", PublicHost, AdminHost));
        Assert.Equal(
            AdminPortalPaths.Login,
            HostRouting.RedirectFor("ADMIN.MuuqWear.COM", "/", PublicHost, "Admin.MuuqWear.com"));
    }

    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/")]
    [InlineData("/admin/orders?tab=orders")]
    [InlineData("/_blazor")]
    [InlineData("/_blazor/negotiate?negotiateVersion=1")]
    [InlineData("/_framework/blazor.server.js")]
    [InlineData("/_content/BlazorBootstrap/css/blazor.bootstrap.css")]
    [InlineData("/auth/staff-login")]
    [InlineData("/auth/clear")]
    [InlineData("/logout")]
    [InlineData("/css/admin-shell.css")]
    [InlineData("/js/site.js")]
    [InlineData("/lib/jquery.js")]
    [InlineData("/bootstrap/css/bootstrap.min.css")]
    [InlineData("/images/logo.png")]
    [InlineData("/favicon.ico")]
    [InlineData("/apple-touch-icon.png")]
    [InlineData("/MuuqWear.Web.styles.css")]
    [InlineData("/MuuqWear.Web.bundle.scp.css")]
    [InlineData("/app.css")]
    [InlineData("/not-found")]
    [InlineData("/error")]
    [InlineData("/Error")]
    [InlineData("/admin/access-denied")]
    public void Allowed_admin_paths_stay(string path)
    {
        Assert.Null(HostRouting.RedirectFor(AdminHost, path, PublicHost, AdminHost));
    }

    [Theory]
    [InlineData("/shop", "https://muuqwear.com/shop")]
    [InlineData("/login?returnUrl=%2Faffiliate", "https://muuqwear.com/login?returnUrl=%2Faffiliate")]
    [InlineData("/community/post?id=1", "https://muuqwear.com/community/post?id=1")]
    public void Other_admin_host_paths_go_to_the_public_site(string path, string expected)
    {
        Assert.Equal(expected, HostRouting.RedirectFor("Admin.MuuqWear.com", path, PublicHost, AdminHost));
    }

    [Theory]
    [InlineData("/admin", "https://admin.muuqwear.com/admin")]
    [InlineData("/admin/f8e2a947-3c1b-4d9e-b562-7a0f1e8d3c92/login", "https://admin.muuqwear.com/admin/f8e2a947-3c1b-4d9e-b562-7a0f1e8d3c92/login")]
    [InlineData("/admin/orders?tab=orders", "https://admin.muuqwear.com/admin/orders?tab=orders")]
    public void Public_admin_paths_go_to_the_admin_host(string path, string expected)
    {
        Assert.Equal(expected, HostRouting.RedirectFor("MuuqWear.com", path, PublicHost, "ADMIN.muuqwear.com"));
        Assert.Equal(expected, HostRouting.RedirectFor("www.muuqwear.com", path, PublicHost, AdminHost));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/shop")]
    [InlineData("/_blazor")]
    [InlineData("/login")]
    public void Public_non_admin_paths_stay(string path)
    {
        Assert.Null(HostRouting.RedirectFor(PublicHost, path, PublicHost, AdminHost));
    }

    [Fact]
    public void Sign_in_url_uses_admin_host_when_set()
    {
        Assert.Equal(
            "https://admin.muuqwear.com" + AdminPortalPaths.Login,
            HostRouting.AdminSignInUrl(AdminHost, PublicHost));
    }

    [Fact]
    public void Sign_in_url_falls_back_to_public_host()
    {
        Assert.Equal(
            "https://muuqwear.com" + AdminPortalPaths.Login,
            HostRouting.AdminSignInUrl("", PublicHost));
        Assert.Equal(
            "https://muuqwear.com" + AdminPortalPaths.Login,
            HostRouting.AdminSignInUrl(null, null));
    }
}
