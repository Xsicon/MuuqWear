using Microsoft.AspNetCore.Http;

namespace MuuqWear.Application.Shared;

/// <summary>
/// Automatically forwards the affiliate cookie to the backend API
/// Runs server-side - client cannot manipulate
/// </summary>
public class AffiliateCookieHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AffiliateCookieHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get the current HTTP context (user's browser request)
        var httpContext = _httpContextAccessor.HttpContext;

        // Try to read the affiliate cookie
        if (httpContext?.Request.Cookies.TryGetValue("muuq_ref", out var affiliateCode) == true)
        {
            // Add the cookie value as a custom header to the API request
            request.Headers.Add("X-Affiliate-Code", affiliateCode);

            // Log for debugging
            Console.WriteLine($"🔐 [Handler] Forwarding affiliate cookie: {affiliateCode}");
        }
        else
        {
            Console.WriteLine($"🔐 [Handler] No affiliate cookie found");
        }

        // Continue with the request
        return await base.SendAsync(request, cancellationToken);
    }
}
