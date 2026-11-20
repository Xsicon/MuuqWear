using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MuuqWear.Web.Constants;
using MuuqWear.Web.ServerAuth;

namespace MuuqWear.Application.Shared;

public sealed class StaffSessionJwt
{
    public const string RoleClaimType = "app_role";

    private readonly string _secret;

    public StaffSessionJwt(IConfiguration configuration)
    {
        _secret = configuration["Authentication:JwtSecret"]
            ?? throw new InvalidOperationException("Authentication:JwtSecret is not configured.");
        RequireStaffMfa = StaffMfaPolicy.IsRequired(configuration);
    }

    public bool RequireStaffMfa { get; }

    public string Create(string firebaseUid, string email, string role, string? userName, string? mfa = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var now = DateTime.UtcNow;
        var userGuid = StableUserGuid(firebaseUid);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userGuid.ToString()),
            new(ClaimTypes.NameIdentifier, userGuid.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Email, email),
            new(RoleClaimType, role),
            new(ClaimTypes.Role, role),
            new("UserId", firebaseUid)
        };
        var displayName = string.IsNullOrWhiteSpace(userName) ? email : userName;
        if (!string.IsNullOrWhiteSpace(displayName))
            claims.Add(new Claim(ClaimTypes.Name, displayName));
        if (!string.IsNullOrWhiteSpace(mfa))
            claims.Add(new Claim(StaffMfaPolicy.ClaimType, mfa));

        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: now,
            expires: now.AddHours(12),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return Handler().WriteToken(token);
    }

    public ClaimsPrincipal? Validate(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;
        try
        {
            var principal = Handler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.Name
            }, out _);

            var identity = new ClaimsIdentity(
                principal.Claims,
                "StaffSession",
                ClaimTypes.Name,
                ClaimTypes.Role);

            var role = identity.FindFirst(RoleClaimType)?.Value
                ?? identity.FindFirst("role")?.Value
                ?? identity.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrWhiteSpace(role) && identity.FindFirst(ClaimTypes.Role) is null)
                identity.AddClaim(new Claim(ClaimTypes.Role, role));

            // A staff role without a verified second factor never reaches the admin portal.
            if (RequireStaffMfa
                && AdminPortalRoles.IsAllowed(role)
                && identity.FindFirst(StaffMfaPolicy.ClaimType)?.Value != StaffMfaPolicy.Totp)
            {
                foreach (var claim in identity.Claims
                             .Where(c => c.Type is RoleClaimType or "role" or ClaimTypes.Role)
                             .ToList())
                    identity.RemoveClaim(claim);
                identity.AddClaim(new Claim(RoleClaimType, "user"));
                identity.AddClaim(new Claim(ClaimTypes.Role, "user"));
            }

            var email = identity.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? identity.FindFirst("email")?.Value;
            if (!string.IsNullOrWhiteSpace(email) && identity.FindFirst(ClaimTypes.Email) is null)
                identity.AddClaim(new Claim(ClaimTypes.Email, email));

            var name = identity.FindFirst(ClaimTypes.Name)?.Value
                ?? identity.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
                ?? identity.FindFirst("unique_name")?.Value
                ?? identity.FindFirst("name")?.Value;
            if (!string.IsNullOrWhiteSpace(name) && identity.FindFirst(ClaimTypes.Name) is null)
                identity.AddClaim(new Claim(ClaimTypes.Name, name));

            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return null;
        }
    }

    private static JwtSecurityTokenHandler Handler()
    {
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        return handler;
    }

    public static Guid StableUserGuid(string firebaseUid)
    {
        if (Guid.TryParse(firebaseUid, out var parsed))
            return parsed;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("muuqwear-firebase:" + firebaseUid));
        return new Guid(hash.AsSpan(0, 16));
    }
}
