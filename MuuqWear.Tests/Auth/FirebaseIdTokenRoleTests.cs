using System.Text;
using System.Text.Json;
using MuuqWear.Application.Shared;
using Xunit;

namespace MuuqWear.Tests.Auth;

public class FirebaseIdTokenRoleTests
{
    [Fact]
    public void AdminClaimMapsToStaffRole()
    {
        Assert.Equal("admin", FirebaseIdTokenRole.FromAccessToken(Token(new { admin = true })));
    }

    [Fact]
    public void MissingClaimStaysAShopper()
    {
        Assert.Equal("user", FirebaseIdTokenRole.FromAccessToken(Token(new { email = "a@b.com" })));
    }

    private static string Token(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var body = Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"eyJhbGciOiJub25lIn0.{body}.";
    }
}
