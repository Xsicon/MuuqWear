using MuuqWear.Model.Site;
using Xunit;

namespace MuuqWear.Tests.Site;

public class CutoverFreezeTests
{
    [Fact]
    public void CatalogReadsStayOpen()
    {
        Assert.True(CutoverFreeze.Allows("GET", "/api/Product/home"));
        Assert.True(CutoverFreeze.Allows("GET", "/shop"));
    }

    [Fact]
    public void CheckoutAndRegisterPause()
    {
        Assert.False(CutoverFreeze.Allows("POST", "/api/Order/place"));
        Assert.False(CutoverFreeze.Allows("POST", "/api/Payment/create-intent"));
        Assert.False(CutoverFreeze.Allows("POST", "/api/Auth/register"));
        Assert.False(CutoverFreeze.Allows("PUT", "/api/Profile"));
        Assert.False(CutoverFreeze.Allows("POST", "/api/Address"));
    }

    [Fact]
    public void SignInStillWorks()
    {
        Assert.True(CutoverFreeze.Allows("POST", "/api/Auth/login"));
        Assert.True(CutoverFreeze.Allows("POST", "/api/Auth/refresh-token"));
        Assert.True(CutoverFreeze.Allows("POST", "/auth/complete"));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("false", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void Enabled_reads_flag(string? flag, bool expected)
    {
        Assert.Equal(expected, CutoverFreeze.Enabled(flag));
    }

    [Fact]
    public void AffiliateSubmitPausesWhenFrozen()
    {
        Assert.False(CutoverFreeze.Allows("POST", "/api/Affiliate"));
        Assert.False(CutoverFreeze.Allows("POST", "/api/Affiliate/apply"));
    }
}
