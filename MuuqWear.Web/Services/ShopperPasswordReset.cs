using Microsoft.AspNetCore.WebUtilities;
using MuuqWear.Application.Shared;

namespace MuuqWear.Web.Services;

public sealed class ShopperPasswordReset
{
    public const string ActionUrl = "https://muuqwear.com/auth/action";
    public const string TooManyMessage = "Too many attempts. Try again in a few minutes.";
    public const string UnableMessage = "Unable to send the reset email.";

    private readonly IShopperResetAccount _accounts;
    private readonly IShopperResetMail _mail;
    private readonly PasswordResetThrottle _throttle;
    private readonly IHttpContextAccessor _http;
    private readonly TimeProvider _time;
    private readonly TimeSpan _minimumResponse;

    public ShopperPasswordReset(
        IShopperResetAccount accounts,
        IShopperResetMail mail,
        PasswordResetThrottle throttle,
        IHttpContextAccessor http,
        TimeProvider time,
        TimeSpan minimumResponse)
    {
        _accounts = accounts;
        _mail = mail;
        _throttle = throttle;
        _http = http;
        _time = time;
        _minimumResponse = minimumResponse;
    }

    public Task<Response<int>> SendAsync(string? email, CancellationToken cancellationToken) =>
        SendAsync(email, _http.HttpContext?.Connection.RemoteIpAddress?.ToString(), cancellationToken);

    public async Task<Response<int>> SendAsync(string? email, string? clientKey, CancellationToken cancellationToken)
    {
        email = email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Response<int>.Fail("Email is required");

        var now = _time.GetUtcNow();
        var emailKey = "email:" + email.ToLowerInvariant();
        var callerKey = "client:" + (string.IsNullOrWhiteSpace(clientKey) ? "unknown" : clientKey);
        if (!_throttle.TryConsume(emailKey, PasswordResetThrottle.EmailLimit, now)
            || !_throttle.TryConsume(callerKey, PasswordResetThrottle.ClientLimit, now))
            return Response<int>.Fail(TooManyMessage);

        var started = _time.GetTimestamp();
        try
        {
            var account = await _accounts.FindAsync(email, cancellationToken);
            if (account is null)
                return await FinishAsync(Response<int>.SuccessResponse(1, "Check your email"), started, cancellationToken);

            await _mail.SendShopperPasswordResetAsync(account.Email, account.Name, account.ResetLink, cancellationToken);
            return await FinishAsync(Response<int>.SuccessResponse(1, "Check your email"), started, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Response<int>.Fail(UnableMessage);
        }
    }

    public static string ToActionUrl(string firebaseLink, string? apiKeyFallback = null)
    {
        if (!Uri.TryCreate(firebaseLink, UriKind.Absolute, out var uri))
            return firebaseLink;

        var values = Read(uri);
        if (string.IsNullOrWhiteSpace(values.OobCode))
            return firebaseLink;

        var query = new List<KeyValuePair<string, string?>>
        {
            new("mode", string.IsNullOrWhiteSpace(values.Mode) ? "resetPassword" : values.Mode),
            new("oobCode", values.OobCode)
        };
        var apiKey = string.IsNullOrWhiteSpace(values.ApiKey) ? apiKeyFallback : values.ApiKey;
        if (!string.IsNullOrWhiteSpace(apiKey))
            query.Add(new("apiKey", apiKey));
        return QueryHelpers.AddQueryString(ActionUrl, query);
    }

    private async Task<Response<int>> FinishAsync(Response<int> result, long started, CancellationToken cancellationToken)
    {
        if (_minimumResponse > TimeSpan.Zero)
        {
            var remaining = _minimumResponse - _time.GetElapsedTime(started);
            if (remaining > TimeSpan.Zero)
                await Task.Delay(remaining, _time, cancellationToken);
        }

        return result;
    }

    private static (string? Mode, string? OobCode, string? ApiKey) Read(Uri uri)
    {
        var query = QueryHelpers.ParseQuery(uri.Query);
        var mode = First(query, "mode");
        var oob = First(query, "oobCode");
        var apiKey = First(query, "apiKey");
        var nested = First(query, "link");
        if (!string.IsNullOrWhiteSpace(nested) && Uri.TryCreate(nested, UriKind.Absolute, out var inner))
        {
            var innerQuery = QueryHelpers.ParseQuery(inner.Query);
            mode ??= First(innerQuery, "mode");
            oob ??= First(innerQuery, "oobCode");
            apiKey ??= First(innerQuery, "apiKey");
        }

        return (mode, oob, apiKey);
    }

    private static string? First(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query, string key) =>
        query.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.ToString() : null;
}
