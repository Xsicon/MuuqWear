using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MuuqWear.Web.ServerAuth;

public enum StaffPasswordOutcome
{
    Invalid,
    SignedIn,
    MfaRequired
}

public sealed record StaffPasswordResult(
    StaffPasswordOutcome Outcome,
    string? IdToken = null,
    string? Email = null,
    string? PendingCredential = null,
    string? EnrollmentId = null);

public sealed record StaffEnrollStart(bool Ok, string? SecretKey = null, string? SessionInfo = null, string? Error = null);

public sealed record StaffMfaResult(bool Ok, string? IdToken = null, string? Error = null);

// Server-side Firebase Identity Toolkit calls for staff TOTP sign-in and enrollment.
public sealed class StaffMfaService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public StaffMfaService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<StaffPasswordResult> SignInWithPasswordAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var apiKey = ApiKey();
        if (apiKey is null || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return new(StaffPasswordOutcome.Invalid);

        var body = await PostAsync<SignInResponse>(
            V1("accounts:signInWithPassword", apiKey),
            new { email, password, returnSecureToken = true },
            cancellationToken);
        if (body is null)
            return new(StaffPasswordOutcome.Invalid);

        if (!string.IsNullOrWhiteSpace(body.MfaPendingCredential))
        {
            var totp = body.MfaInfo?.FirstOrDefault(info => info.TotpInfo is not null);
            return totp?.MfaEnrollmentId is { Length: > 0 } enrollmentId
                ? new(StaffPasswordOutcome.MfaRequired,
                    Email: body.Email ?? email,
                    PendingCredential: body.MfaPendingCredential,
                    EnrollmentId: enrollmentId)
                : new(StaffPasswordOutcome.Invalid);
        }

        return string.IsNullOrWhiteSpace(body.IdToken)
            ? new(StaffPasswordOutcome.Invalid)
            : new(StaffPasswordOutcome.SignedIn, body.IdToken, body.Email ?? email);
    }

    public async Task<StaffMfaResult> FinalizeSignInAsync(
        string pendingCredential,
        string enrollmentId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var apiKey = ApiKey();
        if (apiKey is null)
            return new(false, Error: "config");

        var body = await PostAsync<TokenResponse>(
            V2("accounts/mfaSignIn:finalize", apiKey),
            new
            {
                mfaPendingCredential = pendingCredential,
                mfaEnrollmentId = enrollmentId,
                totpVerificationInfo = new { verificationCode = code }
            },
            cancellationToken);
        return ToResult(body);
    }

    public async Task<StaffEnrollStart> StartEnrollAsync(string idToken, CancellationToken cancellationToken = default)
    {
        var apiKey = ApiKey();
        if (apiKey is null)
            return new(false, Error: "config");

        var body = await PostAsync<EnrollStartResponse>(
            V2("accounts/mfaEnrollment:start", apiKey),
            new { idToken, totpEnrollmentInfo = new { } },
            cancellationToken);
        var info = body?.TotpSessionInfo;
        if (info?.SharedSecretKey is { Length: > 0 } && info.SessionInfo is { Length: > 0 })
            return new(true, info.SharedSecretKey, info.SessionInfo);
        return new(false, Error: ErrorCode(body?.Error?.Message));
    }

    public async Task<StaffMfaResult> FinalizeEnrollAsync(
        string idToken,
        string sessionInfo,
        string code,
        CancellationToken cancellationToken = default)
    {
        var apiKey = ApiKey();
        if (apiKey is null)
            return new(false, Error: "config");

        var body = await PostAsync<TokenResponse>(
            V2("accounts/mfaEnrollment:finalize", apiKey),
            new
            {
                idToken,
                displayName = "Authenticator app",
                totpVerificationInfo = new { sessionInfo, verificationCode = code }
            },
            cancellationToken);
        return ToResult(body);
    }

    public static string OtpAuthUri(string email, string secretKey)
    {
        const string issuer = "MuuqWear Admin";
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}" +
               $"?secret={Uri.EscapeDataString(secretKey)}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";
    }

    // Maps Firebase error messages to the short codes the login page understands.
    public static string ErrorCode(string? firebaseMessage)
    {
        var message = firebaseMessage ?? "";
        if (message.StartsWith("UNVERIFIED_EMAIL", StringComparison.OrdinalIgnoreCase))
            return "unverified";
        if (message.StartsWith("OPERATION_NOT_ALLOWED", StringComparison.OrdinalIgnoreCase)
            || message.Contains("TOTP", StringComparison.OrdinalIgnoreCase) && message.Contains("not enabled", StringComparison.OrdinalIgnoreCase))
            return "mfa-disabled";
        if (message.StartsWith("TOO_MANY_ATTEMPTS", StringComparison.OrdinalIgnoreCase))
            return "too-many";
        if (message.StartsWith("INVALID_MFA_PENDING_CREDENTIAL", StringComparison.OrdinalIgnoreCase)
            || message.StartsWith("TOKEN_EXPIRED", StringComparison.OrdinalIgnoreCase)
            || message.StartsWith("INVALID_ID_TOKEN", StringComparison.OrdinalIgnoreCase)
            || message.StartsWith("INVALID_SESSION_INFO", StringComparison.OrdinalIgnoreCase))
            return "expired";
        return "code";
    }

    private static StaffMfaResult ToResult(TokenResponse? body) =>
        string.IsNullOrWhiteSpace(body?.IdToken)
            ? new(false, Error: ErrorCode(body?.Error?.Message))
            : new(true, body.IdToken);

    private string? ApiKey()
    {
        var key = _config["Firebase:ApiKey"];
        return string.IsNullOrWhiteSpace(key) ? null : key;
    }

    private async Task<T?> PostAsync<T>(Uri url, object payload, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            using var response = await _http.PostAsJsonAsync(url, payload, cancellationToken);
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or NotSupportedException)
        {
            return null;
        }
    }

    private static Uri V1(string method, string apiKey) =>
        new($"https://identitytoolkit.googleapis.com/v1/{method}?key={Uri.EscapeDataString(apiKey)}");

    private static Uri V2(string method, string apiKey) =>
        new($"https://identitytoolkit.googleapis.com/v2/{method}?key={Uri.EscapeDataString(apiKey)}");

    private class ErrorEnvelope
    {
        [JsonPropertyName("error")] public ErrorBody? Error { get; set; }
    }

    private sealed class ErrorBody
    {
        [JsonPropertyName("message")] public string? Message { get; set; }
    }

    private sealed class SignInResponse : ErrorEnvelope
    {
        [JsonPropertyName("idToken")] public string? IdToken { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("mfaPendingCredential")] public string? MfaPendingCredential { get; set; }
        [JsonPropertyName("mfaInfo")] public List<MfaInfo>? MfaInfo { get; set; }
    }

    private sealed class MfaInfo
    {
        [JsonPropertyName("mfaEnrollmentId")] public string? MfaEnrollmentId { get; set; }
        [JsonPropertyName("totpInfo")] public object? TotpInfo { get; set; }
    }

    private sealed class TokenResponse : ErrorEnvelope
    {
        [JsonPropertyName("idToken")] public string? IdToken { get; set; }
    }

    private sealed class EnrollStartResponse : ErrorEnvelope
    {
        [JsonPropertyName("totpSessionInfo")] public TotpSessionInfo? TotpSessionInfo { get; set; }
    }

    private sealed class TotpSessionInfo
    {
        [JsonPropertyName("sharedSecretKey")] public string? SharedSecretKey { get; set; }
        [JsonPropertyName("sessionInfo")] public string? SessionInfo { get; set; }
    }
}
