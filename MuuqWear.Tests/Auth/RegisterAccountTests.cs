using MuuqWear.Client.Auth;
using Xunit;

namespace MuuqWear.Tests.Auth;

public class RegisterAccountTests
{
    [Fact]
    public void SignupPending_UsesAStableBrowserKey()
    {
        Assert.Equal("mw-pending-signup-email", SignupPending.StorageKey);
    }
}
