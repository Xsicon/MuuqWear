using MuuqWear.Model.NotificationModel;
using MuuqWear.Web.Constants;

namespace MuuqWear.Web.Services;

/// <summary>
/// Maps admin header notification types to portal sections for RBAC filtering.
/// </summary>
public static class AdminHeaderNotificationAccess
{
    public static bool CanSeeCustomerNotes(string? role) =>
        AdminPortalRoles.CanAccess(role, AdminPortalSection.Support)
        || AdminPortalRoles.CanAccess(role, AdminPortalSection.Customers);

    public static bool CanSeeNotificationType(string? role, string notificationType)
    {
        var section = GetSectionForNotificationType(notificationType);
        return section != null && AdminPortalRoles.CanAccess(role, section);
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

    public static string? ResolveLink(string? role, string? link) =>
        !string.IsNullOrWhiteSpace(link) && AdminPortalRouteAccess.CanAccessPath(role, link)
            ? link
            : null;

    public static string? GetCustomerNotesListLink(string? role) =>
        ResolveLink(role, "/admin/customers?view=notes");
}
