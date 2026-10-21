using System.Net.Http.Headers;

namespace MuuqWear.Client.Auth;

public static class FirebaseAuthorization
{
    public static void Apply(HttpRequestMessage request, string? idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            request.Headers.Authorization = null;
            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
    }
}
