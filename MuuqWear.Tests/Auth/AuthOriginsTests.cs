using Microsoft.Extensions.Configuration;
using MuuqWear.Web.ServerAuth;
using Xunit;

namespace MuuqWear.Tests.Auth;

public class AuthOriginsTests
{
    [Fact]
    public void AllowlistIncludesConfiguredPublicAndFirebaseHosts()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicHost"] = "muuqwear.com",
                ["FrontendBaseUrl"] = "https://muuqwear-6e2e0.web.app",
                ["Firebase:AuthDomain"] = "muuqwear-6e2e0.firebaseapp.com"
            })
            .Build();

        var hosts = AuthOrigins.AllowedHosts(config);

        Assert.Contains("muuqwear.com", hosts);
        Assert.Contains("www.muuqwear.com", hosts);
        Assert.Contains("muuqwear-6e2e0.web.app", hosts);
        Assert.Contains("muuqwear-6e2e0.firebaseapp.com", hosts);
        Assert.Contains("localhost", hosts);
        Assert.DoesNotContain("evil.example", hosts);
    }
}
