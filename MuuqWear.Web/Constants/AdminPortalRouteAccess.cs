namespace MuuqWear.Web.Constants;

/// <summary>
/// Maps admin URL paths to portal sections for RBAC checks.
/// </summary>
public static class AdminPortalRouteAccess
{
    public static string? GetSectionFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var normalized = path.Split('?', '#')[0].Trim().TrimEnd('/').ToLowerInvariant();

        if (normalized is "" or "/")
            return null;

        if (normalized is "/admin")
            return AdminPortalSection.Overview;

        if (normalized.StartsWith("/admin/orders", StringComparison.Ordinal))
            return AdminPortalSection.Orders;

        if (normalized.StartsWith("/admin/customers", StringComparison.Ordinal))
            return AdminPortalSection.Customers;

        if (normalized.StartsWith("/admin/products", StringComparison.Ordinal))
            return AdminPortalSection.Products;

        if (normalized.StartsWith("/admin/content", StringComparison.Ordinal))
            return AdminPortalSection.Content;

        if (normalized.StartsWith("/admin/affiliates", StringComparison.Ordinal))
            return AdminPortalSection.Affiliates;

        if (normalized.StartsWith("/admin/support", StringComparison.Ordinal)
            || normalized.StartsWith("/admin/live-chat", StringComparison.Ordinal)
            || normalized.StartsWith("/admin/tickets", StringComparison.Ordinal))
            return AdminPortalSection.Support;

        if (normalized.StartsWith("/admin/careers", StringComparison.Ordinal)
            || normalized.StartsWith("/admin/jobs", StringComparison.Ordinal))
            return AdminPortalSection.Careers;

        if (normalized.StartsWith("/admin/system", StringComparison.Ordinal))
            return AdminPortalSection.System;

        if (normalized.StartsWith("/admin/access-denied", StringComparison.Ordinal))
            return AdminPortalSection.Overview;

        return null;
    }

    public static bool CanAccessPath(string? role, string? path)
    {
        var section = GetSectionFromPath(path);
        return section != null && AdminPortalRoles.CanAccess(role, section);
    }
}
