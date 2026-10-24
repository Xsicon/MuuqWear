namespace MuuqWear.Web.ServerAuth;

public static class AuthOrigins
{
    public static bool IsAllowed(HttpContext ctx)
    {
        var config = ctx.RequestServices.GetRequiredService<IConfiguration>();
        var env = ctx.RequestServices.GetRequiredService<IHostEnvironment>();
        var allowed = AllowedHosts(config);

        var origin = ctx.Request.Headers.Origin.FirstOrDefault();
        if (TryHost(origin, out var originHost))
            return allowed.Contains(originHost);

        var referer = ctx.Request.Headers.Referer.FirstOrDefault();
        if (TryHost(referer, out var refererHost))
            return allowed.Contains(refererHost);

        return env.IsDevelopment();
    }

    public static HashSet<string> AllowedHosts(IConfiguration config)
    {
        var hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "localhost",
            "127.0.0.1"
        };
        Add(hosts, config["PublicHost"]);
        Add(hosts, config["FrontendBaseUrl"]);
        Add(hosts, config["Firebase:AuthDomain"]);
        if (hosts.Contains("muuqwear.com"))
            hosts.Add("www.muuqwear.com");
        return hosts;
    }

    public static async ValueTask<object?> RequireSameOrigin(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        if (!IsAllowed(context.HttpContext))
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        return await next(context);
    }

    private static void Add(HashSet<string> hosts, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        var trimmed = value.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            hosts.Add(uri.Host);
        else
            hosts.Add(trimmed.Split(':')[0].Trim());
    }

    private static bool TryHost(string? url, out string host)
    {
        host = "";
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;
        if (uri.Scheme is not ("https" or "http"))
            return false;
        host = uri.Host;
        return !string.IsNullOrWhiteSpace(host);
    }
}
