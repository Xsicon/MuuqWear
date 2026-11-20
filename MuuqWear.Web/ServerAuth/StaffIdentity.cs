using System.IdentityModel.Tokens.Jwt;
using MuuqWear.Application.Shared;

namespace MuuqWear.Web.ServerAuth;

// Reads who signed in from an ID token the server just received from Firebase over TLS.
public sealed record StaffIdentity(string Uid, string Email, string Name, string Role)
{
    public static StaffIdentity FromIdToken(string? idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            return new("", "", "", "user");

        try
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(idToken);
            string Claim(string type) => token.Claims.FirstOrDefault(c => c.Type == type)?.Value ?? "";

            var uid = Claim("user_id");
            if (string.IsNullOrWhiteSpace(uid))
                uid = Claim("sub");
            return new(uid, Claim("email"), Claim("name"), FirebaseIdTokenRole.FromAccessToken(idToken));
        }
        catch (Exception)
        {
            return new("", "", "", "user");
        }
    }
}
