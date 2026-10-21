using MuuqWear.Client.Auth;
using Xunit;

namespace MuuqWear.Tests.Auth;

public class FirebaseAuthorizationTests
{
    [Fact]
    public void Apply_LeavesGuestRequestsWithoutAToken()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://example.test/checkout");

        FirebaseAuthorization.Apply(request, null);
        FirebaseAuthorization.Apply(request, " ");

        Assert.Null(request.Headers.Authorization);
    }

    [Fact]
    public void Apply_SendsTheFirebaseIdToken()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://example.test/checkout");

        FirebaseAuthorization.Apply(request, "firebase-id-token");

        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("firebase-id-token", request.Headers.Authorization?.Parameter);
    }
}
