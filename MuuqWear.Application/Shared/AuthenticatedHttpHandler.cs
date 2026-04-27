using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace MuuqWear.Application.Shared;

public class AuthenticatedHttpHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticatedHttpHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // get access token from cookie claims ✅
        var token = _httpContextAccessor.HttpContext?.User
            .FindFirst("AccessToken")?.Value;

        // attach token to request ✅
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}