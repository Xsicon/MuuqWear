namespace MuuqWear.Web.Constants;

public static class AdminPortalRoles
{
    public const string Admin = "admin";
    public const string OperationsManager = "operations_manager";
    public const string SupportTeam = "support_team";
    public const string Merchandising = "merchandising";
    public const string ContentTeam = "content_team";
    public const string TechnologySystems = "technology_systems";

    public const string All =
        "admin,operations_manager,support_team,merchandising,content_team,technology_systems";

    private static readonly HashSet<string> AllowedStaffRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        Admin,
        OperationsManager,
        SupportTeam,
        Merchandising,
        ContentTeam,
        TechnologySystems
    };

    private static readonly IReadOnlyDictionary<string, string> DisplayNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Admin] = "Admin",
            [OperationsManager] = "Operations Manager",
            [SupportTeam] = "Customer Support",
            [Merchandising] = "Merchandising",
            [ContentTeam] = "Creative & Content",
            [TechnologySystems] = "Technology & Systems"
        };

    private static readonly IReadOnlySet<string> AllSections =
        AdminPortalSection.All.ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> SectionsByRole =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [Admin] = AllSections,
            [OperationsManager] = SectionSet(
                AdminPortalSection.Overview,
                AdminPortalSection.Orders,
                AdminPortalSection.Affiliates,
                AdminPortalSection.Careers),
            [SupportTeam] = SectionSet(
                AdminPortalSection.Overview,
                AdminPortalSection.Support),
            [Merchandising] = SectionSet(
                AdminPortalSection.Overview,
                AdminPortalSection.Products),
            [ContentTeam] = SectionSet(
                AdminPortalSection.Overview,
                AdminPortalSection.Content),
            [TechnologySystems] = SectionSet(
                AdminPortalSection.Overview,
                AdminPortalSection.System)
        };

    public static bool IsAllowed(string? role) =>
        !string.IsNullOrWhiteSpace(role) && AllowedStaffRoles.Contains(role);

    public static bool CanAccess(string? role, string section)
    {
        if (string.IsNullOrWhiteSpace(role) || !IsAllowed(role))
            return false;

        var roleKey = role.Trim();
        return SectionsByRole.TryGetValue(roleKey, out var sections)
               && sections.Contains(section);
    }

    public static IReadOnlyList<string> GetSections(string? role)
    {
        if (string.IsNullOrWhiteSpace(role) || !IsAllowed(role))
            return Array.Empty<string>();

        var roleKey = role.Trim();
        if (!SectionsByRole.TryGetValue(roleKey, out var sections))
            return Array.Empty<string>();

        return AdminPortalSection.All
            .Where(sections.Contains)
            .ToList();
    }

    public static string GetDisplayName(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return "Staff";

        var roleKey = role.Trim();
        return DisplayNames.TryGetValue(roleKey, out var displayName)
            ? displayName
            : roleKey;
    }

    public static string GetDefaultHome(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return "/admin";

        var roleKey = role.Trim();

        if (roleKey.Equals(Admin, StringComparison.OrdinalIgnoreCase)
            || roleKey.Equals(OperationsManager, StringComparison.OrdinalIgnoreCase))
            return "/admin";

        if (roleKey.Equals(SupportTeam, StringComparison.OrdinalIgnoreCase))
            return "/admin/support";

        if (roleKey.Equals(Merchandising, StringComparison.OrdinalIgnoreCase))
            return "/admin/products";

        if (roleKey.Equals(ContentTeam, StringComparison.OrdinalIgnoreCase))
            return "/admin/content";

        if (roleKey.Equals(TechnologySystems, StringComparison.OrdinalIgnoreCase))
            return "/admin/system";

        return "/admin";
    }

    public static string GetWelcomeMessage(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return "Welcome to the Muuqwear internal dashboard.";

        var roleKey = role.Trim();

        if (roleKey.Equals(Admin, StringComparison.OrdinalIgnoreCase))
            return "You have full access to all dashboard features and settings.";
        if (roleKey.Equals(OperationsManager, StringComparison.OrdinalIgnoreCase))
            return "Manage sales, affiliates, and career operations from your dashboard.";
        if (roleKey.Equals(SupportTeam, StringComparison.OrdinalIgnoreCase))
            return "Handle customer support tickets and live chat from your dashboard.";
        if (roleKey.Equals(Merchandising, StringComparison.OrdinalIgnoreCase))
            return "Manage products and inventory from your dashboard.";
        if (roleKey.Equals(ContentTeam, StringComparison.OrdinalIgnoreCase))
            return "Manage site content and creative assets from your dashboard.";
        if (roleKey.Equals(TechnologySystems, StringComparison.OrdinalIgnoreCase))
            return "Monitor system health and technology settings from your dashboard.";

        return "Welcome to the Muuqwear internal dashboard.";
    }

    private static HashSet<string> SectionSet(params string[] sections) =>
        sections.ToHashSet(StringComparer.OrdinalIgnoreCase);
}
