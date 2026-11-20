using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace MuuqWear.Web.ServerAuth;

public sealed record StaffMfaStep(
    string Kind,
    string Email,
    string? PendingCredential = null,
    string? EnrollmentId = null,
    string? IdToken = null,
    string? SessionInfo = null,
    string? SecretKey = null)
{
    public const string SignIn = "signin";
    public const string Enroll = "enroll";
}

// Carries the half-finished login between requests. Encrypted and short-lived, so it
// works across instances without a second cookie (Firebase Hosting forwards only __session).
public sealed class StaffMfaTicket
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly ITimeLimitedDataProtector _protector;

    public StaffMfaTicket(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("MuuqWear.StaffMfa.v1").ToTimeLimitedDataProtector();
    }

    public string Protect(StaffMfaStep step) =>
        _protector.Protect(JsonSerializer.Serialize(step), Lifetime);

    public StaffMfaStep? Unprotect(string? ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket))
            return null;
        try
        {
            var step = JsonSerializer.Deserialize<StaffMfaStep>(_protector.Unprotect(ticket));
            return step is { Kind: StaffMfaStep.SignIn or StaffMfaStep.Enroll } ? step : null;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException)
        {
            return null;
        }
    }
}
