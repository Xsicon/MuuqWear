using MuuqWear.Client.Auth;
using Xunit;

namespace MuuqWear.Tests.Auth;

public class AuthenticatorMfaTests
{
    [Fact]
    public void CustomerAuthenticatorApp_IsOfferedAndNotRequired()
    {
        Assert.False(AuthenticatorMfa.IsRequired);
        Assert.Equal("Optional. Not required.", AuthenticatorMfa.OfferLine);
        Assert.Contains("optional", AuthenticatorMfa.SettingsHelp, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not required", AuthenticatorMfa.SettingsHelp, StringComparison.OrdinalIgnoreCase);
    }
}
