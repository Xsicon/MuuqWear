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
                rememberMe);
        }

        var firebase = await _firebase.VerifyAsync(token, cancellationToken);
        if (firebase is null || string.IsNullOrWhiteSpace(firebase.Uid))
            return null;

        return VerifiedAuthSession.Create(
            token,
            firebase.Uid,
            firebase.Email,
            firebase.Name,
            VerifiedAuthSession.RoleFromClaims(firebase.Claims),
            returnUrl,
            rememberMe,
            refreshToken);
    }
}
