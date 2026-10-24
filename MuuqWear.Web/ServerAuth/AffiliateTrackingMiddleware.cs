using Microsoft.AspNetCore.Http;
using MuuqWear.Application.Services.AffiliateService;
using MuuqWear.Model.AffiliateApplication;

namespace MuuqWear.Application.Shared;

public class AffiliateTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private const string AffiliateCookieName = "muuq_ref";
    private const int CookieDays = 30;

    public AffiliateTrackingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IAffiliateService affiliateService)
    {
        try
        {
            if (context.Request.Query.TryGetValue("ref", out var refCode))
            {
                var affiliateCode = refCode.ToString().Trim().ToUpperInvariant();
                if (!string.IsNullOrEmpty(affiliateCode)
                    && !IsBot(context.Request.Headers.UserAgent.ToString()))
                {
                    var validation = await affiliateService.ValidateAffiliateCode(affiliateCode);
                    if (validation.Success && validation.Data)
                    {
                        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                        var recent = ipAddress != "unknown"
                            && (await affiliateService.HasRecentClick(affiliateCode, ipAddress)).Data;
                        if (!recent)
                        {
                            await affiliateService.TrackClick(new TrackClickRequestModel
                            {
                                AffiliateCode = affiliateCode,
                                IpAddress = ipAddress,
                                UserAgent = context.Request.Headers.UserAgent.ToString(),
                                ReferrerUrl = context.Request.Headers.Referer.ToString()
                            });
                        }

                        SetAffiliateCookie(context, affiliateCode);
                    }
                }
            }
        }
        catch
        {
        }

        await _next(context);
    }

    private static bool IsBot(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return true;

        var botPatterns = new[]
        {
            "bot", "crawler", "spider", "scraper",
            "curl", "wget", "python", "java",
            "http", "libwww", "perl", "php",
            "slurp", "mediapartners", "googlebot",
            "bingbot", "yandex", "baidu"
        };
        var lowerAgent = userAgent.ToLowerInvariant();
        return botPatterns.Any(pattern => lowerAgent.Contains(pattern));
    }

    private static void SetAffiliateCookie(HttpContext context, string affiliateCode)
    {
        context.Response.Cookies.Append(AffiliateCookieName, affiliateCode, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(CookieDays),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }
}
