using System.Text.Json;

namespace MuuqWear.Web.ServerAuth;

public static class StaffMfaPolicy
{
    public const string ClaimType = "mfa";
    public const string Totp = "totp";
    public const string SettingKey = "Authentication:RequireStaffMfa";

    public static bool IsRequired(IConfiguration configuration) =>
        configuration.GetValue(SettingKey, true);

    // Guards code paths that copy a refreshed role onto an existing principal.
    public static bool MayAssignRole(System.Security.Claims.ClaimsPrincipal user, string? role, bool required) =>
        !required
        || !MuuqWear.Web.Constants.AdminPortalRoles.IsAllowed(role)
        || user.FindFirst(ClaimType)?.Value == Totp;

    public static bool FirebaseHasSecondFactor(IReadOnlyDictionary<string, object>? claims)
    {
        if (claims is null || !claims.TryGetValue("firebase", out var firebase) || firebase is null)
            return false;
        return !string.IsNullOrWhiteSpace(ReadSecondFactor(firebase));
    }

    private static string? ReadSecondFactor(object firebase)
    {
        const string key = "sign_in_second_factor";
        switch (firebase)
        {
            case IReadOnlyDictionary<string, object> map:
                return map.TryGetValue(key, out var value) ? value?.ToString() : null;
            case IDictionary<string, object> map:
                return map.TryGetValue(key, out var value2) ? value2?.ToString() : null;
            case JsonElement element:
                return FromJson(element);
        }

        try
        {
            using var doc = JsonDocument.Parse(firebase.ToString() ?? "");
            return FromJson(doc.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }

        static string? FromJson(JsonElement element) =>
            element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(key, out var factor)
            && factor.ValueKind == JsonValueKind.String
                ? factor.GetString()
                : null;
    }
}
