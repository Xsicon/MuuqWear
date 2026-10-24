using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;
using MuuqWear.Application.Shared;
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
    }

    private static Task SignInAsync(HttpContext ctx, CookieAuthSession session) =>
        ctx.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CookieAuthHelper.CreatePrincipal(session),
            CookieAuthHelper.CreateProperties(session));
}
