using System.IdentityModel.Tokens.Jwt;
using MuuqWear.Web.Constants;

namespace MuuqWear.Application.Shared;

public static class FirebaseIdTokenRole
{
    public static string FromAccessToken(string? jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            return "user";

        try
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
            if (token.Claims.Any(claim =>
                    claim.Type == "admin" &&
                    (string.Equals(claim.Value, "true", StringComparison.OrdinalIgnoreCase)
                     || claim.Value == "1")))
            {
                return AdminPortalRoles.Admin;
            }

            var role = token.Claims.FirstOrDefault(claim =>
                    claim.Type is "role" or "app_role")
                ?.Value;
            return string.IsNullOrWhiteSpace(role) ? "user" : role.Trim();
        }
        catch
        {
            return "user";
        }
    }
}
