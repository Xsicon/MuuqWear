using System.Security.Claims;
using MuuqWear.Application.Shared;
using MuuqWear.Web.Constants;
using MuuqWear.Web.Helpers;

namespace MuuqWear.Web.ServerAuth;

public sealed class CookieSessionVerifier
{
    private readonly StaffSessionJwt _staffJwt;
    private readonly IFirebaseIdTokenVerifier _firebase;

    public CookieSessionVerifier(StaffSessionJwt staffJwt, IFirebaseIdTokenVerifier firebase)
    {
        _staffJwt = staffJwt;
        _firebase = firebase;
    }

    public async Task<CookieAuthSession?> CreateAsync(
        string? token,
        string? returnUrl,
        bool rememberMe,
        string? refreshToken = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var staff = _staffJwt.Validate(token);
        if (staff is not null)
        {
            var role = AdminPortalUserContext.GetRole(staff);
            var uid = staff.FindFirst("UserId")?.Value;
            if (!AdminPortalRoles.IsAllowed(role) || string.IsNullOrWhiteSpace(uid))
                return null;

            return VerifiedAuthSession.Create(
                token,
                uid,
                staff.FindFirst(ClaimTypes.Email)?.Value,
                staff.FindFirst(ClaimTypes.Name)?.Value,
                role,
                returnUrl,
                rememberMe,
                mfa: staff.FindFirst(StaffMfaPolicy.ClaimType)?.Value);
        }

        var firebase = await _firebase.VerifyAsync(token, cancellationToken);
        if (firebase is null || string.IsNullOrWhiteSpace(firebase.Uid))
            return null;

        var secondFactor = StaffMfaPolicy.FirebaseHasSecondFactor(firebase.Claims);
        return VerifiedAuthSession.Create(
            token,
            firebase.Uid,
            firebase.Email,
            firebase.Name,
            RoleFor(firebase.Claims, secondFactor, _staffJwt.RequireStaffMfa),
            returnUrl,
            rememberMe,
            refreshToken,
            secondFactor ? StaffMfaPolicy.Totp : null);
    }

    // Staff accounts that signed in without their authenticator keep shopper access only.
    public static string RoleFor(
        IReadOnlyDictionary<string, object> claims,
        bool secondFactor,
        bool requireStaffMfa)
    {
        var role = VerifiedAuthSession.RoleFromClaims(claims);
        return requireStaffMfa && !secondFactor && AdminPortalRoles.IsAllowed(role) ? "user" : role;
    }
}
