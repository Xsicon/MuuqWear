namespace MuuqWear.Web.Constants;

public static class AdminPortalRoles
{
    public const string All =
        "admin,operations_manager,support_team,merchandising,content_team,technology_systems";

    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "operations_manager",
        "support_team",
        "merchandising",
        "content_team",
        "technology_systems"
    };

    public static bool IsAllowed(string? role) =>
        !string.IsNullOrWhiteSpace(role) && Allowed.Contains(role);
}
