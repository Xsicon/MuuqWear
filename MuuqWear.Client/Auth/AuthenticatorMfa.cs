namespace MuuqWear.Client.Auth;

public static class AuthenticatorMfa
{
    public const bool IsRequired = false;
    public const string OfferLine = "Optional. Not required.";
    public const string SettingsTitle = "Authenticator app";
    public const string SettingsHelp =
        "Add a one-time code from an authenticator app when you sign in. This is optional and is not required for customer accounts.";
}

public sealed class FirebaseMfaStatus
{
    public bool Ok { get; set; }
    public bool Enrolled { get; set; }
    public bool SignedIn { get; set; }
    public string? Message { get; set; }
}

public sealed class FirebaseMfaSetup
{
    public bool Ok { get; set; }
    public string? Message { get; set; }
    public string? SecretKey { get; set; }
    public string? QrCodeUrl { get; set; }
    public bool Enrolled { get; set; }
    public bool MfaRequired { get; set; }
}
