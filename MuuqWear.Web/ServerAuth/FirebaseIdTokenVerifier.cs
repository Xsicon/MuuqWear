using FirebaseAdmin.Auth;

namespace MuuqWear.Web.ServerAuth;

public sealed class FirebaseIdTokenVerifier : IFirebaseIdTokenVerifier
{
    public async Task<FirebaseIdTokenClaims?> VerifyAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken, cancellationToken);
            return new FirebaseIdTokenClaims(
                decoded.Uid,
                Claim(decoded, "email"),
                Claim(decoded, "name"),
                decoded.Claims);
        }
        catch (FirebaseAuthException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string Claim(FirebaseToken decoded, string key) =>
        decoded.Claims.TryGetValue(key, out var value) ? value?.ToString() ?? "" : "";
}
