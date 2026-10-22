using System.Security.Cryptography;
using FirebaseAdmin.Auth;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Staff;
using MuuqWear.Web.Constants;
using MuuqWear.Web.Helpers;

namespace MuuqWear.Web.Services;

public sealed class FirebaseStaffUserService : IStaffUserService
{
    private readonly StaffWelcomeMail _mail;
    private readonly IHttpContextAccessor _http;
    private readonly IConfiguration _config;

    public FirebaseStaffUserService(StaffWelcomeMail mail, IHttpContextAccessor http, IConfiguration config)
    {
        _mail = mail;
        _http = http;
        _config = config;
    }

    public async Task<Response<List<StaffUserModel>>> ListAsync(CancellationToken cancellationToken = default)
    {
        var users = new List<StaffUserModel>();
        var pager = FirebaseAuth.DefaultInstance.ListUsersAsync(null);
        await foreach (var record in pager.WithCancellation(cancellationToken))
        {
            if (!StaffUserRules.IsStaff(record.CustomClaims))
                continue;
            users.Add(ToModel(record));
        }

        return Response<List<StaffUserModel>>.SuccessResponse(
            users.OrderBy(u => u.DisplayName).ThenBy(u => u.Email).ToList());
    }

    public async Task<Response<StaffUserModel>> GetAsync(string uid, CancellationToken cancellationToken = default)
    {
        try
        {
            var record = await FirebaseAuth.DefaultInstance.GetUserAsync(uid, cancellationToken);
            if (!StaffUserRules.IsStaff(record.CustomClaims))
                return Response<StaffUserModel>.Fail("User not found");
            return Response<StaffUserModel>.SuccessResponse(ToModel(record));
        }
        catch (FirebaseAuthException)
        {
            return Response<StaffUserModel>.Fail("User not found");
        }
    }

    public async Task<Response<StaffUserModel>> CreateAsync(CreateStaffUserModel request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        var name = request.DisplayName.Trim();
        var role = request.Role.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(name))
            return Response<StaffUserModel>.Fail("Email and name are required.");
        if (!AdminPortalRoles.IsAllowed(role))
            return Response<StaffUserModel>.Fail("Choose a valid role.");

        UserRecord record;
        try
        {
            record = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email, cancellationToken);
            await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
            {
                Uid = record.Uid,
                DisplayName = name,
                Disabled = false
            }, cancellationToken);
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
            record = await FirebaseAuth.DefaultInstance.CreateUserAsync(new UserRecordArgs
            {
                Email = email,
                DisplayName = name,
                EmailVerified = true,
                Password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            }, cancellationToken);
        }

        await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(
            record.Uid, StaffUserRules.ClaimsFor(role), cancellationToken);
        await SendResetAsync(email, name, cancellationToken);
        var fresh = await FirebaseAuth.DefaultInstance.GetUserAsync(record.Uid, cancellationToken);
        return Response<StaffUserModel>.SuccessResponse(ToModel(fresh), "We sent a password-reset email.");
    }

    public async Task<Response<StaffUserModel>> UpdateAsync(string uid, UpdateStaffUserModel request, CancellationToken cancellationToken = default)
    {
        var self = StaffUserRules.GuardSelf(ActorUid(), uid);
        if (self is not null)
            return Response<StaffUserModel>.Fail(self);

        UserRecord record;
        try
        {
            record = await FirebaseAuth.DefaultInstance.GetUserAsync(uid, cancellationToken);
        }
        catch (FirebaseAuthException)
        {
            return Response<StaffUserModel>.Fail("User not found");
        }

        var currentRole = StaffUserRules.ReadRole(record.CustomClaims);
        var nextRole = string.IsNullOrWhiteSpace(request.Role) ? currentRole : request.Role.Trim();
        if (!AdminPortalRoles.IsAllowed(nextRole))
            return Response<StaffUserModel>.Fail("Choose a valid role.");

        var disabling = request.Disabled == true && !record.Disabled;
        var demoting = StaffUserRules.IsAdminClaim(record.CustomClaims)
            && !nextRole.Equals(AdminPortalRoles.Admin, StringComparison.OrdinalIgnoreCase);
        if (disabling || demoting)
        {
            var last = StaffUserRules.GuardLastAdmin(true, true, await CountAdminsAsync(cancellationToken));
            if (last is not null)
                return Response<StaffUserModel>.Fail(last);
        }

        var args = new UserRecordArgs { Uid = uid };
        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            args.DisplayName = request.DisplayName.Trim();
        if (request.Disabled is bool disabled)
            args.Disabled = disabled;
        await FirebaseAuth.DefaultInstance.UpdateUserAsync(args, cancellationToken);
        await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(
            uid, StaffUserRules.ClaimsFor(nextRole), cancellationToken);
        var fresh = await FirebaseAuth.DefaultInstance.GetUserAsync(uid, cancellationToken);
        return Response<StaffUserModel>.SuccessResponse(ToModel(fresh));
    }

    public async Task<Response<bool>> SendPasswordResetAsync(string uid, CancellationToken cancellationToken = default)
    {
        try
        {
            var record = await FirebaseAuth.DefaultInstance.GetUserAsync(uid, cancellationToken);
            if (string.IsNullOrWhiteSpace(record.Email))
                return Response<bool>.Fail("This account has no email.");
            await SendResetAsync(record.Email, record.DisplayName ?? record.Email, cancellationToken);
            return Response<bool>.SuccessResponse(true, "We sent a password-reset email.");
        }
        catch (FirebaseAuthException)
        {
            return Response<bool>.Fail("User not found");
        }
    }

    private async Task SendResetAsync(string email, string name, CancellationToken cancellationToken)
    {
        var site = (_config["FrontendBaseUrl"] ?? "https://muuqwear.com").TrimEnd('/');
        var link = await FirebaseAuth.DefaultInstance.GeneratePasswordResetLinkAsync(
            email,
            new ActionCodeSettings
            {
                Url = site + AdminPortalPaths.Login,
                HandleCodeInApp = false
            },
            cancellationToken);
        await _mail.SendPasswordResetAsync(email, name, link, cancellationToken);
    }

    private async Task<int> CountAdminsAsync(CancellationToken cancellationToken)
    {
        var count = 0;
        var pager = FirebaseAuth.DefaultInstance.ListUsersAsync(null);
        await foreach (var record in pager.WithCancellation(cancellationToken))
        {
            if (!record.Disabled && StaffUserRules.IsAdminClaim(record.CustomClaims))
                count++;
        }
        return count;
    }

    private string ActorUid() =>
        AdminHeaderUserScope.GetUserId(_http.HttpContext?.User ?? new System.Security.Claims.ClaimsPrincipal());

    private static StaffUserModel ToModel(UserRecord record)
    {
        var role = StaffUserRules.ReadRole(record.CustomClaims);
        if (string.IsNullOrWhiteSpace(role) && StaffUserRules.IsAdminClaim(record.CustomClaims))
            role = AdminPortalRoles.Admin;
        return new StaffUserModel
        {
            Uid = record.Uid,
            Email = record.Email ?? "",
            DisplayName = string.IsNullOrWhiteSpace(record.DisplayName) ? record.Email ?? "" : record.DisplayName,
            Role = role,
            Disabled = record.Disabled,
            CreatedAt = record.UserMetaData.CreationTimestamp,
            LastSignInAt = record.UserMetaData.LastSignInTimestamp
        };
    }
}
