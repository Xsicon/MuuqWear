using MuuqWear.Client.Offline;
using Xunit;

namespace MuuqWear.Tests.Offline;

public class OfflineActionTests
{
    [Theory]
    [InlineData("contact")]
    [InlineData("newsletter")]
    [InlineData("chat")]
    [InlineData("wishlist")]
    [InlineData("account")]
    public void Writes_QueueUntilReconnect(string kind)
    {
        Assert.True(OfflineAction.Queues(kind));
        Assert.False(OfflineAction.StaysOnline(kind));
    }

    [Theory]
    [InlineData("sign-in")]
    [InlineData("payment")]
    public void SignInAndPayment_StayOnline(string kind)
    {
        Assert.True(OfflineAction.StaysOnline(kind));
        Assert.False(OfflineAction.Queues(kind));
    }
}
