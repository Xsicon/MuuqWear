using Microsoft.AspNetCore.Http;
using MuuqWear.Web.Services;
using Xunit;

namespace MuuqWear.Tests;

public class ShopperResetLinkTests
{
    [Fact]
    public void Rewrites_firebase_reset_link_onto_the_shop_action_page()
    {
        var firebase = "https://muuqwear-6e2e0.firebaseapp.com/__/auth/action?mode=resetPassword&oobCode=abc&apiKey=key&continueUrl=https%3A%2F%2Fmuuqwear.com%2Fauth%2Faction&lang=en";

        var link = ShopperPasswordReset.ToActionUrl(firebase);

        Assert.Equal("https://muuqwear.com/auth/action?mode=resetPassword&oobCode=abc&apiKey=key", link);
    }

    [Fact]
    public void Reads_a_wrapped_link_and_fills_a_missing_api_key()
    {
        var inner = Uri.EscapeDataString("https://muuqwear-6e2e0.firebaseapp.com/__/auth/action?mode=resetPassword&oobCode=abc");
        var firebase = "https://muuqwear.com/auth/action?link=" + inner;

        var link = ShopperPasswordReset.ToActionUrl(firebase, "web-key");

        Assert.Contains("oobCode=abc", link);
        Assert.Contains("apiKey=web-key", link);
        Assert.StartsWith("https://muuqwear.com/auth/action?", link);
    }

    [Fact]
    public async Task Unknown_address_reports_success_and_sends_nothing()
    {
        var mail = new FakeResetMail();
        var reset = CreateReset(new FakeResetAccounts(null), mail);

        var result = await reset.SendAsync("missing@muuqwear.com", "203.0.113.8", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(0, mail.Calls);
    }

    [Fact]
    public async Task Mail_failure_is_reported()
    {
        var mail = new FakeResetMail { Fail = true };
        var reset = CreateReset(new FakeResetAccounts(SampleAccount()), mail);

        var result = await reset.SendAsync("ada@muuqwear.com", "203.0.113.9", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(ShopperPasswordReset.UnableMessage, result.Message);
    }

    [Fact]
    public async Task Fourth_request_for_one_address_is_refused()
    {
        var mail = new FakeResetMail();
        var reset = CreateReset(new FakeResetAccounts(SampleAccount()), mail);

        for (var i = 0; i < PasswordResetThrottle.EmailLimit; i++)
            Assert.True((await reset.SendAsync("ada@muuqwear.com", "203.0.113.10", CancellationToken.None)).Success);

        var blocked = await reset.SendAsync("Ada@muuqwear.com", "203.0.113.10", CancellationToken.None);

        Assert.False(blocked.Success);
        Assert.Equal(ShopperPasswordReset.TooManyMessage, blocked.Message);
        Assert.Equal(PasswordResetThrottle.EmailLimit, mail.Calls);
    }

    [Fact]
    public async Task Ninth_address_from_one_client_is_refused()
    {
        var mail = new FakeResetMail();
        var reset = CreateReset(new FakeResetAccounts(SampleAccount()), mail);

        for (var i = 0; i < PasswordResetThrottle.ClientLimit; i++)
            Assert.True((await reset.SendAsync($"person{i}@muuqwear.com", "203.0.113.11", CancellationToken.None)).Success);

        var blocked = await reset.SendAsync("extra@muuqwear.com", "203.0.113.11", CancellationToken.None);

        Assert.False(blocked.Success);
        Assert.Contains("too many", blocked.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(PasswordResetThrottle.ClientLimit, mail.Calls);
    }

    private static ShopperResetAccount SampleAccount() =>
        new("ada@muuqwear.com", "Ada", "https://muuqwear.com/auth/action?mode=resetPassword&oobCode=abc&apiKey=key");

    private static ShopperPasswordReset CreateReset(IShopperResetAccount accounts, IShopperResetMail mail) =>
        new(accounts, mail, new PasswordResetThrottle(), new HttpContextAccessor(), TimeProvider.System, TimeSpan.Zero);

    private sealed class FakeResetAccounts : IShopperResetAccount
    {
        private readonly ShopperResetAccount? _account;
        public FakeResetAccounts(ShopperResetAccount? account) => _account = account;
        public Task<ShopperResetAccount?> FindAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(_account);
    }

    private sealed class FakeResetMail : IShopperResetMail
    {
        public int Calls { get; private set; }
        public bool Fail { get; set; }
        public Task SendShopperPasswordResetAsync(string email, string name, string link, CancellationToken cancellationToken)
        {
            Calls++;
            if (Fail)
                throw new InvalidOperationException("postmark");
            return Task.CompletedTask;
        }
    }
}
