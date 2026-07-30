namespace MuuqWear.Web.Components.Pages.AdminComponent.Support;

public static class SupportTicketTeams
{
    public const string Unassigned = "Unassigned";

    public static readonly string[] All =
    [
        Unassigned,
        "Support Team",
        "Billing Team",
        "Shipping Team",
        "Escalations"
    ];
}

public static class SupportTicketAgents
{
    public const string Unassigned = "Unassigned";

    public static readonly string[] DefaultAgents =
    [
        Unassigned,
        "Sarah Chen",
        "Priya Sharma",
        "Alex Morgan",
        "Marcus Ali"
    ];

    public static IReadOnlyList<string> WithCurrentUser(string? currentUserName)
    {
        if (string.IsNullOrWhiteSpace(currentUserName))
            return DefaultAgents;

        var trimmed = currentUserName.Trim();
        if (DefaultAgents.Any(a =>
                a.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            return DefaultAgents;
        }

        return [.. DefaultAgents, trimmed];
    }
}

public static class SupportTicketStatuses
{
    public static readonly (string Value, string Label)[] All =
    [
        ("open", "Open"),
        ("in_progress", "In Progress"),
        ("resolved", "Resolved")
    ];
}

public static class SupportTicketPriorities
{
    public static readonly (string Value, string Label)[] All =
    [
        ("low", "Low"),
        ("normal", "Normal"),
        ("high", "High")
    ];
}
