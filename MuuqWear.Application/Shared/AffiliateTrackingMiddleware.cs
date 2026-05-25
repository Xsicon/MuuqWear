using Microsoft.AspNetCore.Http;
using MuuqWear.Application.Services.AffiliateService;
using MuuqWear.Model.AffiliateApplication;

namespace MuuqWear.Application.Shared;

public class AffiliateTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private const string AFFILIATE_COOKIE_NAME = "muuq_ref";
    private const int COOKIE_DAYS = 30;

    public AffiliateTrackingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAffiliateService affiliateService)
    {
        try
        {
            // Check if there's a ?ref= parameter in the URL
            if (context.Request.Query.TryGetValue("ref", out var refCode))
            {
                var affiliateCode = refCode.ToString().Trim().ToUpper();

                if (!string.IsNullOrEmpty(affiliateCode))
                {
                    // 🛡️ ANTI-SPAM CHECK #1: Cookie exists?
                    if (context.Request.Cookies.ContainsKey(AFFILIATE_COOKIE_NAME))
                    {
                        var existingCode = context.Request.Cookies[AFFILIATE_COOKIE_NAME];

                        // Same code = already tracked, just refresh cookie
                        if (existingCode == affiliateCode)
                        {
                            Console.WriteLine($"🔄 Cookie exists for {affiliateCode}, refreshing expiry only");
                            SetAffiliateCookie(context, affiliateCode);
                            await _next(context);
                            return;
                        }

                        // Different code = new affiliate, allow tracking
                        Console.WriteLine($"🔀 Switching from {existingCode} to {affiliateCode}");
                    }

                    // 🛡️ ANTI-SPAM CHECK #2: Bot detection
                    var userAgent = context.Request.Headers["User-Agent"].ToString();
                    if (IsBot(userAgent))
                    {
                        Console.WriteLine($"🤖 Bot detected: {userAgent}");
                        await _next(context);
                        return;
                    }

                    // 🛡️ ANTI-SPAM CHECK #3: Recent IP check
                    var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    if (ipAddress != "unknown")
                    {
                        var recentClickCheck = await affiliateService.HasRecentClick(affiliateCode, ipAddress);
                        if (recentClickCheck.Success && recentClickCheck.Data)
                        {
                            Console.WriteLine($"⏰ Recent click from IP {ipAddress} for {affiliateCode}, skipping");

                            // Still set cookie for attribution, but don't count click
                            SetAffiliateCookie(context, affiliateCode);
                            await _next(context);
                            return;
                        }
                    }

                    //  ALL CHECKS PASSED - Validate and track
                    var validationResponse = await affiliateService.ValidateAffiliateCode(affiliateCode);

                    if (validationResponse.Success && validationResponse.Data)
                    {
                        Console.WriteLine($" Tracking new click for {affiliateCode}");

                        // Track the click
                        var trackRequest = new TrackClickRequestModel
                        {
                            AffiliateCode = affiliateCode,
                            IpAddress = ipAddress,
                            UserAgent = userAgent,
                            ReferrerUrl = context.Request.Headers["Referer"].ToString()
                        };

                        await affiliateService.TrackClick(trackRequest);

                        // Set 30-day cookie
                        SetAffiliateCookie(context, affiliateCode);
                    }
                    else
                    {
                        Console.WriteLine($" Invalid affiliate code: {affiliateCode}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log error but don't break the request
            Console.WriteLine($"Affiliate tracking error: {ex.Message}");
        }

        // Continue to next middleware
        await _next(context);
    }

    private bool IsBot(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return true;

        // Common bot user-agent patterns
        var botPatterns = new[]
        {
        "bot", "crawler", "spider", "scraper",
        "curl", "wget", "python", "java",
        "http", "libwww", "perl", "php",
        "slurp", "mediapartners", "googlebot",
        "bingbot", "yandex", "baidu"
    };

        var lowerAgent = userAgent.ToLower();
        return botPatterns.Any(pattern => lowerAgent.Contains(pattern));
    }
    private void SetAffiliateCookie(HttpContext context, string affiliateCode)
    {
        var cookieOptions = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(COOKIE_DAYS),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        };

        context.Response.Cookies.Append(AFFILIATE_COOKIE_NAME, affiliateCode, cookieOptions);
    }
}
