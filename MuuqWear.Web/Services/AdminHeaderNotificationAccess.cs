using MuuqWear.Model.NotificationModel;
using MuuqWear.Web.Constants;

namespace MuuqWear.Web.Services;

/// <summary>
/// Maps admin header notification types to portal sections for RBAC filtering.
/// </summary>
public static class AdminHeaderNotificationAccess
{
    public static bool CanSeeLiveChat(string? role) =>
        AdminPortalRoles.CanAccess(role, AdminPortalSection.Support);

    public static bool CanSeeCustomerNotes(string? role) =>
        AdminPortalRoles.CanAccess(role, AdminPortalSection.Support)
        || AdminPortalRoles.CanAccess(role, AdminPortalSection.Customers);

    public static bool CanSeeMessages(string? role) =>
        CanSeeCustomerNotes(role) || CanSeeLiveChat(role);

    public static bool CanSeeNotificationType(string? role, string notificationType)
    {
        var section = GetSectionForNotificationType(notificationType);
        if (section != null)
            return AdminPortalRoles.CanAccess(role, section);

        // Unmapped API types must still reach admins so new kinds are not silently dropped.
        return !string.IsNullOrWhiteSpace(role)
               && role.Equals(AdminPortalRoles.Admin, StringComparison.OrdinalIgnoreCase);
    }

    public static string? GetSectionForNotificationType(string type) => type switch
    {
        NotificationType.Order => AdminPortalSection.Orders,
        NotificationType.Affiliate => AdminPortalSection.Affiliates,
        NotificationType.CustomerMessage => AdminPortalSection.Support,
        NotificationType.LowStock or NotificationType.Stock => AdminPortalSection.Products,
        _ when type.Contains("stock", StringComparison.OrdinalIgnoreCase) => AdminPortalSection.Products,
        _ => null
    };

    public static string? ResolveLink(string? role, string? link)
    {
        if (string.IsNullOrWhiteSpace(link))
            return null;

        if (AdminPortalRouteAccess.CanAccessPath(role, link))
            return link;

        // D1: support can see notes but not /admin/customers — land on Support instead of a dead click.
        var section = AdminPortalRouteAccess.GetSectionFromPath(link);
        if (section == AdminPortalSection.Customers
            && CanSeeCustomerNotes(role)
            && AdminPortalRoles.CanAccess(role, AdminPortalSection.Support))
            return "/admin/support";

        return null;
    }

    public static string? GetCustomerNotesListLink(string? role)
    {
        const string customersNotesLink = "/admin/customers?view=notes";
        if (AdminPortalRouteAccess.CanAccessPath(role, customersNotesLink))
            return customersNotesLink;

        if (CanSeeCustomerNotes(role)
            && AdminPortalRoles.CanAccess(role, AdminPortalSection.Support))
            return "/admin/support";

        return null;
    }
}
