using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;
using MuuqWear.Application.Services.AuthService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Authentication;
using MuuqWear.Web.Constants;

namespace MuuqWear.Web.ServerAuth;

public static class AuthCookieEndpoints
{
    private const string CachePrefix = "auth-cookie:";

    public static void MapAuthCookies(this WebApplication app)
    {
        app.MapPost("/auth/sign-in", async (
            HttpContext ctx,
            CookieAuthSession body,
            CookieSessionVerifier verifier,
            AuthSessionService authSession,
            CancellationToken cancellationToken) =>
        {
            var session = await verifier.CreateAsync(
                body.Token,
                body.ReturnUrl,
                body.RememberMe == true,
                body.RefreshToken,
                cancellationToken);
            if (session is null)
                return Results.Unauthorized();

            authSession.Reset();
            await SignInAsync(ctx, session);
            return Results.Ok(new { success = true });
        }).AddEndpointFilter(AuthOrigins.RequireSameOrigin).DisableAntiforgery();

        app.MapPost("/auth/complete", async (HttpContext ctx, CookieSessionVerifier verifier) =>
        {
            var ticket = "";
            var returnUrl = "";
            if (ctx.Request.HasFormContentType)
            {
                ticket = ctx.Request.Form["ticket"].ToString();
                returnUrl = ctx.Request.Form["returnUrl"].ToString();
            }

            var session = await verifier.CreateAsync(ticket, returnUrl, rememberMe: true, cancellationToken: ctx.RequestAborted);
            if (session is null || !AdminPortalRoles.IsAllowed(session.Role))
                return Results.Redirect("/admin/access-denied");

            await SignInAsync(ctx, session);
            return Results.Redirect(session.ReturnUrl ?? AdminPortalRoles.GetDefaultHome(session.Role));
        }).AddEndpointFilter(AuthOrigins.RequireSameOrigin).DisableAntiforgery();

        app.MapMethods("/auth/complete", [HttpMethods.Get, HttpMethods.Head], () =>
            Results.StatusCode(StatusCodes.Status405MethodNotAllowed));

        app.MapPost("/auth/prepare-cookie", async (
            CookieAuthSession body,
            CookieSessionVerifier verifier,
            IMemoryCache cache,
            CancellationToken cancellationToken) =>
        {
            var session = await verifier.CreateAsync(
                body.Token,
                body.ReturnUrl,
                body.RememberMe == true,
                body.RefreshToken,
                cancellationToken);
            if (session is null)
                return Results.Unauthorized();

            var key = Guid.NewGuid().ToString("N");
            cache.Set(CachePrefix + key, session, TimeSpan.FromSeconds(30));
            return Results.Ok(key);
        }).AddEndpointFilter(AuthOrigins.RequireSameOrigin).DisableAntiforgery();

        app.MapGet("/auth/set-cookie", async (
            HttpContext ctx,
            string key,
            IMemoryCache cache,
            AuthSessionService authSession,
            bool json = false) =>
        {
            if (!cache.TryGetValue(CachePrefix + key, out CookieAuthSession? session) || session is null)
            {
                return json
                    ? Results.Unauthorized()
                    : Results.Redirect("/login");
            }

            cache.Remove(CachePrefix + key);
            authSession.Reset();
            await SignInAsync(ctx, session);

            return json
                ? Results.Ok(new { success = true })
                : Results.Redirect(session.ReturnUrl ?? "/profile");
        }).AddEndpointFilter(AuthOrigins.RequireSameOrigin).DisableAntiforgery();

        // Firebase Hosting forwards only the __session cookie, so the Blazor
        // antiforgery cookie never arrives and the login form POST returns 400.
        app.MapPost("/auth/staff-login", async (
            HttpContext ctx,
            StaffMfaService mfa,
            StaffMfaTicket tickets,
            StaffSessionJwt staffJwt) =>
        {
            if (!ctx.Request.HasFormContentType)
                return LoginError("invalid");

            var form = await ctx.Request.ReadFormAsync();
            var result = await mfa.SignInWithPasswordAsync(
                form["email"].ToString().Trim(),
                form["password"].ToString(),
                ctx.RequestAborted);

            switch (result.Outcome)
            {
                case StaffPasswordOutcome.MfaRequired:
                    return StepRedirect("code", tickets.Protect(new StaffMfaStep(
                        StaffMfaStep.SignIn,
                        result.Email ?? "",
                        PendingCredential: result.PendingCredential,
                        EnrollmentId: result.EnrollmentId)));

                case StaffPasswordOutcome.SignedIn:
                    var identity = StaffIdentity.FromIdToken(result.IdToken);
                    if (!AdminPortalRoles.IsAllowed(identity.Role))
                        return LoginError("denied");

                    if (!staffJwt.RequireStaffMfa)
                        return await CompleteStaffSignInAsync(ctx, staffJwt, identity, mfa: null);

                    // No authenticator yet: enrollment is part of signing in.
                    var enroll = await mfa.StartEnrollAsync(result.IdToken!, ctx.RequestAborted);
                    if (!enroll.Ok)
                        return LoginError(enroll.Error ?? "invalid");

                    return StepRedirect("enroll", tickets.Protect(new StaffMfaStep(
                        StaffMfaStep.Enroll,
                        identity.Email,
                        IdToken: result.IdToken,
                        SessionInfo: enroll.SessionInfo,
                        SecretKey: enroll.SecretKey)));

                default:
                    return LoginError("invalid");
            }
        }).AddEndpointFilter(AuthOrigins.RequireSameOrigin).DisableAntiforgery();

        app.MapPost("/auth/staff-login/verify", async (
            HttpContext ctx,
            StaffMfaService mfa,
            StaffMfaTicket tickets,
            StaffSessionJwt staffJwt) =>
        {
            if (!ctx.Request.HasFormContentType)
                return LoginError("expired");

            var form = await ctx.Request.ReadFormAsync();
            var ticket = form["t"].ToString();
            var step = tickets.Unprotect(ticket);
            if (step is null)
                return LoginError("expired");

            var stepName = step.Kind == StaffMfaStep.Enroll ? "enroll" : "code";
            var code = new string(form["code"].ToString().Where(char.IsDigit).ToArray());
            if (code.Length != 6)
                return StepRedirect(stepName, ticket, "code");

            var verified = step.Kind == StaffMfaStep.Enroll
                ? await mfa.FinalizeEnrollAsync(step.IdToken ?? "", step.SessionInfo ?? "", code, ctx.RequestAborted)
                : await mfa.FinalizeSignInAsync(step.PendingCredential ?? "", step.EnrollmentId ?? "", code, ctx.RequestAborted);

            if (!verified.Ok)
            {
                return verified.Error is "expired" or "config"
                    ? LoginError("expired")
                    : StepRedirect(stepName, ticket, verified.Error ?? "code");
            }

            var identity = StaffIdentity.FromIdToken(verified.IdToken);
            if (!AdminPortalRoles.IsAllowed(identity.Role))
                return LoginError("denied");

            return await CompleteStaffSignInAsync(ctx, staffJwt, identity, StaffMfaPolicy.Totp);
        }).AddEndpointFilter(AuthOrigins.RequireSameOrigin).DisableAntiforgery();
    }

    private static IResult LoginError(string error) =>
        Results.Redirect(AdminPortalPaths.Login + "?error=" + Uri.EscapeDataString(error));

    private static IResult StepRedirect(string step, string ticket, string? error = null) =>
        Results.Redirect(
            $"{AdminPortalPaths.Login}?step={step}&t={Uri.EscapeDataString(ticket)}" +
            (error is null ? "" : "&error=" + Uri.EscapeDataString(error)));

    private static async Task<IResult> CompleteStaffSignInAsync(
        HttpContext ctx,
        StaffSessionJwt staffJwt,
        StaffIdentity identity,
        string? mfa)
    {
        if (string.IsNullOrWhiteSpace(identity.Uid))
            return LoginError("invalid");

        var home = AdminPortalRoles.GetDefaultHome(identity.Role);
        await SignInAsync(ctx, new CookieAuthSession
        {
            Token = staffJwt.Create(identity.Uid, identity.Email, identity.Role, identity.Name, mfa),
            Username = identity.Name,
            Email = identity.Email,
            Role = identity.Role,
            UserId = identity.Uid,
            ReturnUrl = home,
            RememberMe = true,
            Mfa = mfa
        });
        return Results.Redirect(home);
    }

    private static Task SignInAsync(HttpContext ctx, CookieAuthSession session) =>
        ctx.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CookieAuthHelper.CreatePrincipal(session),
            CookieAuthHelper.CreateProperties(session));
}
