namespace MuuqWear.Web.ServerAuth;

public sealed record FirebaseIdTokenClaims(
    string Uid,
    string Email,
    string Name,
    IReadOnlyDictionary<string, object> Claims);

public interface IFirebaseIdTokenVerifier
{
    Task<FirebaseIdTokenClaims?> VerifyAsync(string idToken, CancellationToken cancellationToken = default);
}
